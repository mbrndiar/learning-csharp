namespace LearningCSharp.Course.Unit14.Practice.Domain;

public sealed record ReadingSummary(
    int TotalBooks,
    int TotalPages,
    double AverageRating,
    IReadOnlyList<string> RecommendedTitles);
