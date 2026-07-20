using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Tasks.Core;

namespace Tasks.Storage;

/// <summary>
/// Versioned Markdown checklist implementation of the shared task repository.
/// Every operation parses and validates the complete document before exposing
/// data, mutations publish a complete same-directory replacement, and a
/// per-path lock serializes load-modify-save cycles within this process.
/// </summary>
public sealed partial class MarkdownTaskRepository : ITaskRepository
{
    private const int FormatVersion = 1;
    private const string Heading = "# Tasks";

    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new(StringComparer.Ordinal);

    private readonly string _documentPath;
    private readonly SemaphoreSlim _lock;

    /// <summary>Bind the repository to one document path without touching the file.</summary>
    public MarkdownTaskRepository(string documentPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(documentPath);
        _documentPath = Path.GetFullPath(documentPath);
        _lock = Locks.GetOrAdd(_documentPath, static _ => new SemaphoreSlim(1, 1));
    }

    /// <inheritdoc />
    public async Task<TaskItem> CreateAsync(CreateTaskInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        const string operation = "create";
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Document document = await LoadAsync(operation, cancellationToken).ConfigureAwait(false);
            var created = new TaskItem(document.NextId, input.Title, false);
            var tasks = new List<TaskItem>(document.Tasks) { created };
            await SaveAsync(new Document(document.NextId + 1, tasks), operation, cancellationToken)
                .ConfigureAwait(false);
            return created;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TaskItem>> ListAsync(
        bool? completed = null,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            IReadOnlyList<TaskItem> tasks = (await LoadAsync("list", cancellationToken).ConfigureAwait(false)).Tasks;
            if (completed is null)
            {
                return tasks.ToArray();
            }

            return tasks.Where(task => task.Completed == completed.Value).ToArray();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<TaskItem> GetAsync(long taskId, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Document document = await LoadAsync("get", cancellationToken).ConfigureAwait(false);
            return document.Tasks.FirstOrDefault(task => task.Id == taskId)
                ?? throw new TaskNotFoundException(taskId);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<TaskItem> UpdateAsync(
        long taskId,
        UpdateTaskInput update,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(update);
        const string operation = "update";
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Document document = await LoadAsync(operation, cancellationToken).ConfigureAwait(false);
            var tasks = new List<TaskItem>(document.Tasks);
            for (int index = 0; index < tasks.Count; index++)
            {
                if (tasks[index].Id != taskId)
                {
                    continue;
                }

                TaskItem updated = update.ApplyTo(tasks[index]);
                tasks[index] = updated;
                await SaveAsync(new Document(document.NextId, tasks), operation, cancellationToken)
                    .ConfigureAwait(false);
                return updated;
            }

            throw new TaskNotFoundException(taskId);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(long taskId, CancellationToken cancellationToken = default)
    {
        const string operation = "delete";
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Document document = await LoadAsync(operation, cancellationToken).ConfigureAwait(false);
            var remaining = document.Tasks.Where(task => task.Id != taskId).ToList();
            if (remaining.Count == document.Tasks.Count)
            {
                throw new TaskNotFoundException(taskId);
            }

            // next-id is retained so a deleted identifier can never be reused.
            await SaveAsync(new Document(document.NextId, remaining), operation, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<Document> LoadAsync(string operation, CancellationToken cancellationToken)
    {
        string text = await ReadTextAsync(operation, cancellationToken).ConfigureAwait(false);
        if (!text.EndsWith('\n') || text.Contains('\r', StringComparison.Ordinal))
        {
            throw StorageError(operation, "document must use LF lines and end with one newline");
        }

        string[] lines = text[..^1].Split('\n');
        if (lines.Length < 2)
        {
            throw StorageError(operation, "document is incomplete");
        }

        Match metadata = MetadataPattern().Match(lines[0]);
        if (!metadata.Success)
        {
            throw StorageError(operation, "metadata comment is malformed");
        }

        int version = int.Parse(metadata.Groups[1].Value, CultureInfo.InvariantCulture);
        if (version != FormatVersion)
        {
            throw StorageError(operation, $"unsupported format version {version}");
        }

        if (!long.TryParse(metadata.Groups[2].Value, NumberStyles.None, CultureInfo.InvariantCulture, out long nextId))
        {
            throw StorageError(operation, "metadata comment is malformed");
        }

        if (!string.Equals(lines[1], Heading, StringComparison.Ordinal))
        {
            throw StorageError(operation, "Tasks heading is malformed");
        }

        string[] rowLines;
        if (lines.Length == 2)
        {
            rowLines = [];
        }
        else if (lines[2].Length != 0)
        {
            throw StorageError(operation, "checklist rows must follow one blank line");
        }
        else
        {
            rowLines = lines[3..];
            if (rowLines.Length == 0)
            {
                throw StorageError(operation, "empty documents must not contain a trailing blank line");
            }
        }

        var tasks = new List<TaskItem>();
        long previousId = 0;
        var seenIds = new HashSet<long>();
        for (int offset = 0; offset < rowLines.Length; offset++)
        {
            int lineNumber = offset + 4;
            Match row = ChecklistPattern().Match(rowLines[offset]);
            if (!row.Success)
            {
                throw StorageError(operation, $"malformed checklist row at line {lineNumber}");
            }

            long taskId = long.Parse(row.Groups[2].Value, CultureInfo.InvariantCulture);
            if (!seenIds.Add(taskId))
            {
                throw StorageError(operation, $"duplicate task ID {taskId}");
            }

            if (taskId < previousId)
            {
                throw StorageError(operation, $"task IDs are out of order at line {lineNumber}");
            }

            string rawTitle = row.Groups[3].Value;
            string title;
            try
            {
                title = TaskValidation.ValidateTitle(rawTitle);
            }
            catch (TaskValidationException error)
            {
                throw StorageError(operation, $"invalid title at line {lineNumber}: {error.Message}");
            }

            if (!string.Equals(title, rawTitle, StringComparison.Ordinal))
            {
                throw StorageError(operation, $"title at line {lineNumber} is not stored literally");
            }

            tasks.Add(new TaskItem(taskId, title, string.Equals(row.Groups[1].Value, "x", StringComparison.Ordinal)));
            previousId = taskId;
        }

        if (tasks.Count > 0 && nextId <= tasks[^1].Id)
        {
            throw StorageError(operation, "next-id must be greater than every stored task ID");
        }

        return new Document(nextId, tasks);
    }

    private async Task<string> ReadTextAsync(string operation, CancellationToken cancellationToken)
    {
        try
        {
            byte[] bytes = await File.ReadAllBytesAsync(_documentPath, cancellationToken).ConfigureAwait(false);
            return Utf8.GetString(bytes);
        }
        catch (FileNotFoundException)
        {
            // A missing path is an uninitialized store, not an existing empty or
            // malformed document; publish the canonical empty representation.
            var document = new Document(1, []);
            await SaveAsync(document, operation, cancellationToken).ConfigureAwait(false);
            return Render(document);
        }
        catch (DirectoryNotFoundException)
        {
            var document = new Document(1, []);
            await SaveAsync(document, operation, cancellationToken).ConfigureAwait(false);
            return Render(document);
        }
        catch (Exception error) when (error is IOException or DecoderFallbackException or UnauthorizedAccessException)
        {
            throw StorageError(operation, error.Message);
        }
    }

    private async Task SaveAsync(Document document, string operation, CancellationToken cancellationToken)
    {
        string? directory = Path.GetDirectoryName(_documentPath);
        string fileName = Path.GetFileName(_documentPath);
        string temporaryPath = Path.Combine(
            directory ?? ".",
            $".{fileName}.{Guid.NewGuid():N}.tmp");

        try
        {
            byte[] bytes = Utf8.GetBytes(Render(document));
            await using (var stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None))
            {
                await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, _documentPath, overwrite: true);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            throw StorageError(operation, error.Message);
        }
        finally
        {
            TryDelete(temporaryPath);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // Cleanup is best effort; a stray temporary must not mask the result.
        }
    }

    private static string Render(Document document)
    {
        var builder = new StringBuilder();
        builder.Append(CultureInfo.InvariantCulture, $"<!-- rest-task-api:v{FormatVersion} next-id={document.NextId} -->\n");
        builder.Append(Heading).Append('\n');
        if (document.Tasks.Count > 0)
        {
            builder.Append('\n');
            foreach (TaskItem task in document.Tasks)
            {
                char mark = task.Completed ? 'x' : ' ';
                builder.Append(CultureInfo.InvariantCulture, $"- [{mark}] {task.Id}: {task.Title}\n");
            }
        }

        return builder.ToString();
    }

    private static TaskStorageException StorageError(string operation, string message)
        => new($"Markdown {operation} failed: {message}", operation);

    [GeneratedRegex(@"\A<!-- rest-task-api:v([1-9][0-9]*) next-id=([1-9][0-9]*) -->\z")]
    private static partial Regex MetadataPattern();

    [GeneratedRegex(@"\A- \[( |x)\] ([1-9][0-9]*): (.+)\z")]
    private static partial Regex ChecklistPattern();

    private sealed record Document(long NextId, IReadOnlyList<TaskItem> Tasks);
}
