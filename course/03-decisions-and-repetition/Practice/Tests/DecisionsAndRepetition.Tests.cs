using LearningCSharp.DecisionsAndRepetition;
using Xunit;

namespace LearningCSharp.DecisionsAndRepetition.Tests;

public sealed class ControlFlowPracticeTests
{
    [Theory]
    [InlineData(-1, "invalid")]
    [InlineData(49, "needs work")]
    [InlineData(50, "pass")]
    [InlineData(70, "good")]
    [InlineData(90, "excellent")]
    [InlineData(100, "excellent")]
    [InlineData(101, "invalid")]
    public void DescribeScoreReturnsExpectedLabelForScoreBoundaries(int score, string expected)
    {
        Assert.Equal(expected, ControlFlowPractice.DescribeScore(score));
    }

    [Theory]
    [InlineData(0, "Lift off!")]
    [InlineData(1, "1, Lift off!")]
    [InlineData(3, "3, 2, 1, Lift off!")]
    public void BuildCountdownReturnsExpectedSequenceForValidStarts(int start, string expected)
    {
        Assert.Equal(expected, ControlFlowPractice.BuildCountdown(start));
    }

    [Fact]
    public void BuildCountdownThrowsArgumentOutOfRangeExceptionForNegativeStart()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ControlFlowPractice.BuildCountdown(-1));
    }
}
