namespace Tasks.Client;

/// <summary>Validated settings and command produced before any network I/O.</summary>
public sealed record ClientRequest(ClientSettings Settings, ClientCommand Command);
