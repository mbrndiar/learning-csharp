namespace ComparativeKv.Tests.Support;

internal sealed class ScenarioDirectory : IDisposable
{
    private bool disposed;

    public ScenarioDirectory(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"comparative-kv-csharp-{name}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path);
        DatabasePath = System.IO.Path.Combine(Path, "store.db");
        MissingParentPath = System.IO.Path.Combine(Path, "missing-parent");
    }

    public string Path { get; }

    public string DatabasePath { get; }

    public string MissingParentPath { get; }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        foreach (var path in new[]
                 {
                     DatabasePath,
                     DatabasePath + "-wal",
                     DatabasePath + "-shm",
                     DatabasePath + "-journal",
                 })
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            if (File.Exists(path))
            {
                throw new IOException($"The scenario database sidecar remained open: {path}");
            }
        }

        Directory.Delete(Path, recursive: true);
        disposed = true;
    }
}
