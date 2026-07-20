namespace Tasks.Tests.Support;

/// <summary>
/// A project-local temporary directory used for deterministic storage. Nothing
/// is written under a system temp path, satisfying environments that prohibit
/// them, and the directory is removed on dispose even when a test fails.
/// </summary>
public sealed class TempDirectory : IDisposable
{
    /// <summary>Create and register a unique directory beneath the test output.</summary>
    public TempDirectory()
    {
        Path = System.IO.Path.Combine(AppContext.BaseDirectory, ".tasks-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    /// <summary>The absolute directory path.</summary>
    public string Path { get; }

    /// <summary>Resolve a file path inside this directory.</summary>
    public string File(string name) => System.IO.Path.Combine(Path, name);

    /// <inheritdoc />
    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
        catch (IOException)
        {
            // Cleanup is best effort; a locked artifact must not fail the test.
        }
    }
}
