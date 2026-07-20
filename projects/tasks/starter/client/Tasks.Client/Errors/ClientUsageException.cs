namespace Tasks.Client;

/// <summary>A command usage error, reported on stderr with exit code 2.</summary>
public sealed class ClientUsageException : Exception
{
    /// <summary>Create a usage error.</summary>
    public ClientUsageException(string message)
        : base(message)
    {
    }
}
