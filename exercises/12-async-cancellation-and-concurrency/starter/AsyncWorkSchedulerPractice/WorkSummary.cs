namespace LearningCSharp.Course.Unit12.Practice;

public sealed record WorkSummary(
    int StartedCount,
    int CompletedCount,
    int TotalValue,
    IReadOnlyList<string> CompletionOrder);
