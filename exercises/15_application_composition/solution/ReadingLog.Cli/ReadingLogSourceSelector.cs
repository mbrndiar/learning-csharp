namespace LearningCSharp.Exercises.ApplicationComposition.Cli;

/// <summary>
/// Pure decision logic for the composition root in Program.cs: given the
/// value of the READINGLOG_SOURCE environment variable, should the HTTP
/// adapter be active instead of the default file adapter? Kept separate from
/// Program.cs (and free of any direct <c>Environment.GetEnvironmentVariable</c>
/// call) so it is testable without mutating real process environment state.
/// </summary>
public static class ReadingLogSourceSelector
{
    public static bool ShouldUseHttpSource(string? readingLogSourceEnvironmentValue) =>
        string.Equals(readingLogSourceEnvironmentValue, "http", StringComparison.OrdinalIgnoreCase);
}
