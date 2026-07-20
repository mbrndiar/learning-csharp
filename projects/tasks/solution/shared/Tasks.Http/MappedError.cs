namespace Tasks.Http;

/// <summary>A mapped HTTP status, error envelope, and optional Allow header.</summary>
public readonly record struct MappedError(int StatusCode, ErrorResponse Body, string? Allow);
