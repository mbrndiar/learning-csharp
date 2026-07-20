namespace Tasks.Http;

/// <summary>Serialized task value returned to clients.</summary>
public sealed record TaskResponse(long Id, string Title, bool Completed);
