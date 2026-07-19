using System.Text.Json;
using ReadingLog.Core;

namespace ReadingLog.Storage.Json;

public sealed class JsonReadingLogRepository : IReadingLogRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    private readonly string _storageDirectory;
    private readonly string _storageFilePath;

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

        _storageDirectory = Path.GetFullPath(options.StorageDirectory);
        _storageFilePath = Path.Combine(_storageDirectory, options.FileName);
    }

    public string StorageFilePath => _storageFilePath;

    public async Task<ReadingLogSnapshot> LoadAsync(CancellationToken cancellationToken)
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
            FileShare.Read,
            4096,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        try
        {
            var document = await JsonSerializer.DeserializeAsync<StorageDocument>(stream, SerializerOptions, cancellationToken);
            return document is null
                ? ReadingLogSnapshot.Empty
                : new ReadingLogSnapshot(document.Books ?? Array.Empty<Book>(), document.Entries ?? Array.Empty<ReadingEntry>());
        }
        catch (JsonException exception)
        {
            // TODO(m2): Replace this with richer malformed-data errors in milestone 2.
            throw new InvalidDataException("TODO(m2): The starter repository does not fully report malformed JSON yet.", exception);
        }
    }

    public async Task SaveAsync(ReadingLogSnapshot snapshot, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(_storageDirectory);

        await using var stream = new FileStream(
            _storageFilePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            4096,
            FileOptions.Asynchronous);

        // TODO(m2): Save atomically through a temporary file in milestone 2.
        await JsonSerializer.SerializeAsync(stream, new StorageDocument(snapshot.Books.ToArray(), snapshot.Entries.ToArray()), SerializerOptions, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    private sealed record StorageDocument(IReadOnlyList<Book>? Books, IReadOnlyList<ReadingEntry>? Entries);
}
