namespace Tasks.Client;

/// <summary>Library-neutral non-HTTP-response failure during one exchange.</summary>
public class TransportException : Exception
{
    /// <summary>Create a transport failure with a stable message.</summary>
    public TransportException(string message)
        : base(message)
    {
    }
}
