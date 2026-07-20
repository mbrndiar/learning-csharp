using System.Collections.Concurrent;
using Tasks.Core;

namespace Tasks.Storage;

/// <summary>
/// Versioned Markdown checklist implementation of the shared task repository.
/// The starter binds the document path and the process-local coordination lock
/// without touching the filesystem; parsing and publication are milestone two.
/// </summary>
public sealed class MarkdownTaskRepository : ITaskRepository
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new(StringComparer.Ordinal);

    private readonly string _documentPath;
    private readonly SemaphoreSlim _lock;

    /// <summary>Bind the repository to one document path without touching the file.</summary>
    public MarkdownTaskRepository(string documentPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(documentPath);
        _documentPath = Path.GetFullPath(documentPath);
        // The per-path lock registry is provided; hold this lock across each
        // complete load/use or load-modify-save cycle in milestone two.
        _lock = Locks.GetOrAdd(_documentPath, static _ => new SemaphoreSlim(1, 1));
    }

    /// <inheritdoc />
    public Task<TaskItem> CreateAsync(CreateTaskInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        _ = _lock;
        return Incomplete.Value<Task<TaskItem>>($"milestone 2 Markdown create in {_documentPath}");
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<TaskItem>> ListAsync(bool? completed = null, CancellationToken cancellationToken = default)
        => Incomplete.Value<Task<IReadOnlyList<TaskItem>>>($"milestone 2 Markdown list in {_documentPath}");

    /// <inheritdoc />
    public Task<TaskItem> GetAsync(long taskId, CancellationToken cancellationToken = default)
        => Incomplete.Value<Task<TaskItem>>($"milestone 2 Markdown get {taskId} in {_documentPath}");

    /// <inheritdoc />
    public Task<TaskItem> UpdateAsync(long taskId, UpdateTaskInput update, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(update);
        return Incomplete.Value<Task<TaskItem>>($"milestone 2 Markdown update {taskId} in {_documentPath}");
    }

    /// <inheritdoc />
    public Task DeleteAsync(long taskId, CancellationToken cancellationToken = default)
        => Incomplete.Value<Task>($"milestone 2 Markdown delete {taskId} in {_documentPath}");
}
