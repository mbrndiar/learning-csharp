namespace LearningCSharp.Exercises.ApplicationComposition.Domain;

public static class ReadingSummaryCalculator
{
    public static ReadingSummary Create(IEnumerable<ReadingEntry> entries, int minimumRating)
    {
        // TODO: Reject a null entry sequence and a minimum rating outside 1..5.
        // TODO: Validate every entry (title present, positive pages, rating in 1..5) as invalid data.
        // TODO: Compute totals, the rounded average rating, and the recommended titles ordered by rating then title.
        throw new NotImplementedException("TODO: Implement the pure domain calculation.");
    }
}
