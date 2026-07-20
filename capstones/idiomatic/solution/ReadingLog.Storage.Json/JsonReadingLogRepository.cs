using System.Text.Json;
using ReadingLog.Core;

namespace ReadingLog.Storage.Json;

public sealed class JsonReadingLogRepository : IReadingLogRepository, IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    private readonly string _storageDirectory;
    private readonly string _storageFilePath;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public JsonReadingLogRepository(JsonReadingLogRepositoryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(options.StorageDirectory))
        {
            throw new ArgumentException("A storage directory is required.", nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.FileName))
        {
            throw new ArgumentException("A storage file name is required.", nameof(options));
        }

        if (Path.IsPathRooted(options.FileName)
            || options.FileName.Contains(Path.DirectorySeparatorChar)
            || options.FileName.Contains(Path.AltDirectorySeparatorChar))
        {
            throw new ArgumentException("FileName must be a simple file name.", nameof(options));
        }

        _storageDirectory = Path.GetFullPath(options.StorageDirectory);
        _storageFilePath = Path.Combine(_storageDirectory, options.FileName);
    }

    public string StorageFilePath => _storageFilePath;

    public void Dispose() => _gate.Dispose();

    public async Task<ReadingLogSnapshot> LoadAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return await LoadCoreAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<ReadingLogSnapshot> LoadCoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!File.Exists(_storageFilePath))
        {
            return ReadingLogSnapshot.Empty;
        }

        var fileInfo = new FileInfo(_storageFilePath);
        if (fileInfo.Length == 0)
        {
            return ReadingLogSnapshot.Empty;
        }

        await using var stream = new FileStream(
            _storageFilePath,
            FileMode.Open,
            FileAccess.Read,
            // Atomic replacement can rename the path while this reader owns the old file.
            FileShare.Read | FileShare.Delete,
            4096,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        StorageDocument? document;
        try
        {
            document = await JsonSerializer.DeserializeAsync<StorageDocument>(stream, SerializerOptions, cancellationToken);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"The storage file '{_storageFilePath}' contains malformed JSON.", exception);
        }

        if (document is null || document.Books is null || document.Entries is null)
        {
            throw new InvalidDataException($"The storage file '{_storageFilePath}' must contain a JSON object with 'books' and 'entries' arrays.");
        }

        var snapshot = new ReadingLogSnapshot(document.Books, document.Entries);
        try
        {
            ReadingLogValidation.ValidateSnapshot(snapshot);
        }
        catch (DomainValidationException exception)
        {
            throw new InvalidDataException($"The storage file '{_storageFilePath}' contains invalid reading log data.", exception);
        }

        return snapshot;
    }

    public async Task SaveAsync(ReadingLogSnapshot snapshot, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            await SaveCoreAsync(snapshot, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task SaveCoreAsync(ReadingLogSnapshot snapshot, CancellationToken cancellationToken)
    {
        ReadingLogValidation.ValidateSnapshot(snapshot);
        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(_storageDirectory);

        var temporaryFilePath = _storageFilePath + ".tmp";
        try
        {
            await using (var stream = new FileStream(
                temporaryFilePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                4096,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                var document = new StorageDocument(snapshot.Books.ToArray(), snapshot.Entries.ToArray());
                await JsonSerializer.SerializeAsync(stream, document, SerializerOptions, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryFilePath, _storageFilePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryFilePath))
            {
                File.Delete(temporaryFilePath);
            }
        }
    }

    private sealed record StorageDocument(IReadOnlyList<Book>? Books, IReadOnlyList<ReadingEntry>? Entries);
}
