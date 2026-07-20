namespace Tasks.Client;

/// <summary>Constructs one transport for a single CLI invocation.</summary>
/// <param name="baseUrl">The validated base URL.</param>
/// <param name="timeout">A positive, finite request timeout.</param>
public delegate ITaskTransport TransportFactory(string baseUrl, TimeSpan timeout);
