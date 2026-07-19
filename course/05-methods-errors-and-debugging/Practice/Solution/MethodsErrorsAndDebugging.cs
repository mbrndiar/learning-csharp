namespace LearningCSharp.MethodsErrorsAndDebugging;

public static class ScoreCalculator
{
    public static double Average(int first, int second) => Average(new[] { first, second });

    public static double Average(int[] scores)
    {
        ArgumentNullException.ThrowIfNull(scores);

        if (scores.Length == 0)
        {
            throw new ArgumentException("Provide at least one score.", nameof(scores));
        }

        int total = 0;
        for (int index = 0; index < scores.Length; index++)
        {
            int score = scores[index];
            if (score < 0 || score > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(scores), score, $"Score at index {index} must be between 0 and 100.");
            }

            total += score;
        }

        return (double)total / scores.Length;
    }

    public static string DescribeAverage(int[] scores)
    {
        double average = Average(scores);
        return average switch
        {
            >= 90.0 => "excellent",
            >= 70.0 => "good",
            >= 50.0 => "pass",
            _ => "needs work",
        };
    }
}
