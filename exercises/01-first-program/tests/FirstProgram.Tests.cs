using LearningCSharp.FirstProgram;
using Xunit;

namespace LearningCSharp.FirstProgram.Tests;

public sealed class FirstProgramExerciseTests
{
    [Fact]
    public void BuildCelebrationMessageReturnsExpectedThreeLinesForTrimmedName()
    {
        string actual = FirstProgramExercise.BuildCelebrationMessage("Ada");
        string expected = "Hello, Ada!"
            + Environment.NewLine + "You have a working C# program."
            + Environment.NewLine + "Change one line, run again, and observe the difference.";

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void BuildCelebrationMessageTrimsSurroundingWhitespace()
    {
        string actual = FirstProgramExercise.BuildCelebrationMessage("  Jo  ");

        Assert.StartsWith("Hello, Jo!", actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildCelebrationMessageThrowsArgumentExceptionForBlankNames(string learnerName)
    {
        Assert.Throws<ArgumentException>(() => FirstProgramExercise.BuildCelebrationMessage(learnerName));
    }
}
