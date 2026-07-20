namespace Tasks.Client;

/// <summary>Update one or both mutable task fields.</summary>
public sealed record UpdateCommand(long TaskId, string? Title, bool? Completed) : ClientCommand;
