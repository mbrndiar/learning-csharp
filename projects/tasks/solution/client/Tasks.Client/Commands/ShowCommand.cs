namespace Tasks.Client;

/// <summary>Fetch one task by identifier.</summary>
public sealed record ShowCommand(long TaskId) : ClientCommand;
