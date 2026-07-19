using System.Globalization;

namespace LearningCSharp.ValuesTypesAndNull;

public static class ReadingProgressFormatter
{
    public static string DescribeProgress(string? title, int pagesRead, int totalPages, double? rating)
    {
        if (pagesRead < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pagesRead), pagesRead, "pagesRead must be zero or greater.");
        }

        if (totalPages < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalPages), totalPages, "totalPages must be zero or greater.");
        }

        if (pagesRead > totalPages)
        {
            throw new ArgumentOutOfRangeException(nameof(pagesRead), pagesRead, "pagesRead cannot be greater than totalPages.");
        }

        if (rating is < 0.0 or > 5.0)
        {
            throw new ArgumentOutOfRangeException(nameof(rating), rating, "rating must be between 0.0 and 5.0 when provided.");
        }

        string safeTitle = string.IsNullOrWhiteSpace(title) ? "(untitled)" : title.Trim();
        double percent = totalPages == 0 ? 0.0 : (double)pagesRead / totalPages * 100;
        string shownRating = rating is null
            ? "unrated"
            : string.Create(CultureInfo.InvariantCulture, $"{rating.Value:0.0}★");

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{safeTitle}: {pagesRead}/{totalPages} pages ({percent:0.0}%), {shownRating}");
    }
}
