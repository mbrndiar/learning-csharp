namespace Tasks.Http;

/// <summary>The shared JSON error envelope.</summary>
public sealed record ErrorResponse(ErrorBody Error);
