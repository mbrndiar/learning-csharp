using Tasks.Core;
using Tasks.Server.Configuration;

namespace Tasks.Server.Persistence;

/// <summary>
/// Constructs the persistence adapter selected at the application edge. This is
/// the only place that maps a <see cref="StorageBackend"/> to a concrete
/// repository, keeping storage choice out of the service and HTTP layers.
/// </summary>
public static class RepositoryFactory
{
    /// <summary>Create the repository for one backend and storage location.</summary>
    public static ITaskRepository Create(StorageBackend backend, string dataPath) => backend switch
    {
        StorageBackend.Sqlite => new SqliteTaskRepository(dataPath),
        StorageBackend.Markdown => new MarkdownTaskRepository(dataPath),
        _ => throw new ArgumentOutOfRangeException(nameof(backend), backend, "unsupported storage backend"),
    };
}
