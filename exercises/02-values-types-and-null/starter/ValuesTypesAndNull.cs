namespace LearningCSharp.ValuesTypesAndNull;

public static class ReadingProgressFormatter
{
    public static string DescribeProgress(string? title, int pagesRead, int totalPages, double? rating)
    {
        // TODO: Reject negative pagesRead or totalPages, pagesRead greater than totalPages, and any
        // rating outside 0.0 through 5.0 (double.NaN must be treated as out of range too).
        // TODO: Use (untitled) when title is missing and unrated when rating is missing.
        // TODO: Return one formatted line with a 0.0 percentage and optional Unicode star.
        throw new NotImplementedException("TODO: describe the reading progress.");
    }
}
