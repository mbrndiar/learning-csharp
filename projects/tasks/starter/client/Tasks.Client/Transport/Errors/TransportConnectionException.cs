namespace Tasks.Client;

/// <summary>The client could not establish or maintain the HTTP exchange.</summary>
public class TransportConnectionException : TransportException
{
    /// <summary>Create a connection failure.</summary>
    public TransportConnectionException(string message)
        : base(message)
    {
    }
}
