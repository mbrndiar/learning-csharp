namespace Tasks.Server.Configuration;

/// <summary>Raised when server launcher arguments are invalid.</summary>
public sealed class ServerConfigurationException : Exception
{
    /// <summary>Create a configuration failure with a user-facing message.</summary>
    public ServerConfigurationException(string message)
        : base(message)
    {
    }
}
