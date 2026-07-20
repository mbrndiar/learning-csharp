namespace LearningCSharp.Exercises.ApplicationComposition.Application;

public sealed record SummaryConfiguration
{
    public string DataFile { get; init; } = string.Empty;

    public int MinimumRating { get; init; } = 4;
}
