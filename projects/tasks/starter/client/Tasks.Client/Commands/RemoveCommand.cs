namespace Tasks.Client;

/// <summary>Delete one task.</summary>
public sealed record RemoveCommand(long TaskId) : ClientCommand;
