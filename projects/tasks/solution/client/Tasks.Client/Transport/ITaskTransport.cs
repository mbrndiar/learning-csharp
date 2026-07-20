namespace Tasks.Client;

/// <summary>
/// Send once per call and release all adapter-owned network resources. Adapters
/// expose only status, headers, and body bytes so the application owns response
/// decoding and validation policy.
/// </summary>
public interface ITaskTransport : IAsyncDisposable
{
    /// <summary>Send one request without retries, preserving non-idempotent semantics.</summary>
    Task<TransportResponse> SendAsync(TransportRequest request, CancellationToken cancellationToken = default);
}
