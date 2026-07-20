namespace Tasks.Client;

/// <summary>Create one task from a title.</summary>
public sealed record AddCommand(string Title) : ClientCommand;
