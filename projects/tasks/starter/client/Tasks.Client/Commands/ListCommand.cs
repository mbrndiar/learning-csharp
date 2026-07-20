namespace Tasks.Client;

/// <summary>List tasks with an optional completion filter.</summary>
public sealed record ListCommand(bool? Completed) : ClientCommand;
