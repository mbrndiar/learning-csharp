namespace LearningCSharp.Exercises.ApplicationComposition.Domain;

public sealed record ReadingSummary(
    int TotalBooks,
    int TotalPages,
    double AverageRating,
    IReadOnlyList<string> RecommendedTitles);
