using System.Globalization;

namespace LearningCSharp.DecisionsAndRepetition;

public static class ControlFlowPractice
{
    public static string DescribeScore(int score) => score switch
    {
        < 0 or > 100 => "invalid",
        >= 90 => "excellent",
        >= 70 => "good",
        >= 50 => "pass",
        _ => "needs work",
    };

    public static string BuildCountdown(int start)
    {
        if (start < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(start), start, "start must be zero or greater.");
        }

        if (start == 0)
        {
            return "Lift off!";
        }

        string countdown = start.ToString(CultureInfo.InvariantCulture);
        int current = start - 1;

        while (current > 0)
        {
            countdown += ", " + current.ToString(CultureInfo.InvariantCulture);
            current--;
        }

        return countdown + ", Lift off!";
    }
}
