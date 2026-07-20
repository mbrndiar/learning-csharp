namespace Tasks.Client;

/// <summary>Mark one task complete.</summary>
public sealed record CompleteCommand(long TaskId) : ClientCommand;
