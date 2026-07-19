using LearningCSharp.Course.Unit14.Practice.Domain;

namespace LearningCSharp.Course.Unit14.Practice.Application;

public sealed class SummaryApplication(IReadingLogSource source)
{
    public async Task<SummaryReport> RunAsync(
        SummaryConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        if (string.IsNullOrWhiteSpace(configuration.DataFile))
        {
            throw new InvalidDataException("Configuration must provide a data file.");
        }

        IReadOnlyList<ReadingEntry> entries = await source.LoadAsync(configuration.DataFile, cancellationToken);
        ReadingSummary summary = ReadingSummaryCalculator.Create(entries, configuration.MinimumRating);
        string recommended = summary.RecommendedTitles.Count == 0
            ? "(none)"
            : string.Join(", ", summary.RecommendedTitles);

        return new SummaryReport(
            summary,
            [
                $"Total books: {summary.TotalBooks}",
                $"Total pages: {summary.TotalPages}",
                $"Average rating: {summary.AverageRating:F2}",
                $"Recommended: {recommended}",
            ]);
    }
}
