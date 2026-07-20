namespace Tasks.Client;

/// <summary>The HTTP exchange exceeded its finite timeout.</summary>
public sealed class TransportTimeoutException : TransportConnectionException
{
    /// <summary>Create a timeout failure.</summary>
    public TransportTimeoutException(string message)
        : base(message)
    {
    }
}
