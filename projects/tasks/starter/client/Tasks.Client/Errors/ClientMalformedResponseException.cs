namespace Tasks.Client;

/// <summary>The response had an unexpected status, content type, or JSON shape.</summary>
public sealed class ClientMalformedResponseException : Exception
{
    /// <summary>Create a malformed-response failure.</summary>
    public ClientMalformedResponseException(string message)
        : base(message)
    {
    }
}
