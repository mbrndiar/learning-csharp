using LearningCSharp.ValuesTypesAndNull;
using Xunit;

namespace LearningCSharp.ValuesTypesAndNull.Tests;

public sealed class ReadingProgressFormatterTests
{
    [Fact]
    public void DescribeProgressFormatsUnicodeTitlePercentageAndRating()
    {
        string actual = ReadingProgressFormatter.DescribeProgress("Café Notes", 12, 30, 4.5);

        Assert.Equal("Café Notes: 12/30 pages (40.0%), 4.5★", actual);
    }

    [Fact]
    public void DescribeProgressUsesFallbacksWhenTitleAndRatingAreMissing()
    {
        string actual = ReadingProgressFormatter.DescribeProgress("   ", 0, 0, null);

        Assert.Equal("(untitled): 0/0 pages (0.0%), unrated", actual);
    }

    [Theory]
    [InlineData(-1, 10, null)]
    [InlineData(11, 10, null)]
    [InlineData(1, 10, -0.1)]
    [InlineData(1, 10, 5.1)]
    public void DescribeProgressThrowsArgumentOutOfRangeExceptionForInvalidValues(
        int pagesRead,
        int totalPages,
        double? rating)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => ReadingProgressFormatter.DescribeProgress("Book", pagesRead, totalPages, rating));
    }
}
