namespace Tasks.Client;

/// <summary>Connection settings shared by every client transport.</summary>
public sealed record ClientSettings(string BaseUrl, TimeSpan Timeout);
