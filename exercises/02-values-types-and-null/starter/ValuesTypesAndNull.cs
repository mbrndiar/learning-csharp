namespace LearningCSharp.ValuesTypesAndNull;

public static class ReadingProgressFormatter
{
    public static string DescribeProgress(string? title, int pagesRead, int totalPages, double? rating)
    {
        // TODO: Implement DescribeProgress to throw ArgumentOutOfRangeException when pagesRead or
        // totalPages is negative, when pagesRead is greater than totalPages, or when rating is
        // provided and is outside 0.0 through 5.0 (double.NaN must also be treated as out of range).
        // TODO: Implement DescribeProgress to fall back to (untitled) when title is null or
        // whitespace-only, and to unrated when rating is null.
        // TODO: Implement DescribeProgress to return
        // "<title>: <read>/<total> pages (<percentage with one decimal>%), <rating with one decimal>★",
        // replacing the final rating/star with "unrated" when no rating is present.
        throw new NotImplementedException("TODO: describe the reading progress.");
    }
}
