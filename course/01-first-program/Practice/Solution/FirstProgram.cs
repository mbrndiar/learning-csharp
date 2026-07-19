namespace LearningCSharp.FirstProgram;

public static class FirstProgramExercise
{
    public static string BuildCelebrationMessage(string learnerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(learnerName);

        string safeName = learnerName.Trim();
        return "Hello, " + safeName + "!"
            + Environment.NewLine + "You have a working C# program."
            + Environment.NewLine + "Change one line, run again, and observe the difference.";
    }
}
