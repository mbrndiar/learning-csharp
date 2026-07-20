namespace Tasks.Client;

/// <summary>The server returned a documented API error.</summary>
public sealed class ClientApiException : Exception
{
    /// <summary>Create an API failure with the observed status and code.</summary>
    public ClientApiException(int status, string code, string message)
        : base(message)
    {
        Status = status;
        Code = code;
    }

    /// <summary>The HTTP status returned.</summary>
    public int Status { get; }

    /// <summary>The stable error code returned.</summary>
    public string Code { get; }
}
