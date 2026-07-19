namespace LearningCSharp.Course.Unit14.Practice.Domain;

public static class ReadingSummaryCalculator
{
    public static ReadingSummary Create(IEnumerable<ReadingEntry> entries, int minimumRating)
    {
        ArgumentNullException.ThrowIfNull(entries);
        if (minimumRating is < 1 or > 5)
        {
            throw new InvalidDataException("MinimumRating must be between 1 and 5.");
        }

        ReadingEntry[] materialized = entries.ToArray();
        foreach (ReadingEntry entry in materialized)
        {
            if (string.IsNullOrWhiteSpace(entry.Title))
            {
                throw new InvalidDataException("Each reading entry needs a title.");
            }

            if (entry.Pages <= 0)
            {
                throw new InvalidDataException("Pages must be positive.");
            }

            if (entry.Rating is < 1 or > 5)
            {
                throw new InvalidDataException("Ratings must be between 1 and 5.");
            }
        }

        int totalPages = materialized.Sum(entry => entry.Pages);
        double averageRating = materialized.Length == 0
            ? 0
            : Math.Round(materialized.Average(entry => entry.Rating), 2, MidpointRounding.AwayFromZero);
        IReadOnlyList<string> recommendedTitles = materialized
            .Where(entry => entry.Rating >= minimumRating)
            .OrderByDescending(entry => entry.Rating)
            .ThenBy(entry => entry.Title, StringComparer.Ordinal)
            .Select(entry => entry.Title)
            .ToArray();

        return new ReadingSummary(materialized.Length, totalPages, averageRating, recommendedTitles);
    }
}
