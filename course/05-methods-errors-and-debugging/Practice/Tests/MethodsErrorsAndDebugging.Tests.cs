using LearningCSharp.MethodsErrorsAndDebugging;
using Xunit;

namespace LearningCSharp.MethodsErrorsAndDebugging.Tests;

public sealed class ScoreCalculatorTests
{
    [Fact]
    public void AverageTwoArgumentOverloadReusesAveragingLogic()
    {
        Assert.Equal(85.0, ScoreCalculator.Average(80, 90));
    }

    [Fact]
    public void AverageArrayOverloadReturnsFractionalAverageForValidScores()
    {
        int[] scores = [70, 80];

        Assert.Equal(75.0, ScoreCalculator.Average(scores));
    }

    [Fact]
    public void AverageArrayOverloadAllowsSingleBoundaryScore()
    {
        int[] scores = [100];

        Assert.Equal(100.0, ScoreCalculator.Average(scores));
    }

    [Theory]
    [InlineData(new[] { 95, 90 }, "excellent")]
    [InlineData(new[] { 75, 70 }, "good")]
    [InlineData(new[] { 60, 50 }, "pass")]
    [InlineData(new[] { 20, 40 }, "needs work")]
    public void DescribeAverageReturnsExpectedCategory(int[] scores, string expected)
    {
        Assert.Equal(expected, ScoreCalculator.DescribeAverage(scores));
    }

    [Fact]
    public void AverageArrayOverloadThrowsArgumentNullExceptionForNullArray()
    {
        Assert.Throws<ArgumentNullException>(() => ScoreCalculator.Average(null!));
    }

    [Fact]
    public void AverageArrayOverloadThrowsArgumentExceptionForEmptyArray()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => ScoreCalculator.Average(Array.Empty<int>()));

        Assert.Equal("scores", exception.ParamName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void AverageArrayOverloadThrowsArgumentOutOfRangeExceptionForInvalidScore(int score)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ScoreCalculator.Average(new[] { score }));
    }
}
