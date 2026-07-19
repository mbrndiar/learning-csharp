using System.Globalization;

int[] threeScores = new int[] { 70, 80, 90 };

Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Average of two scores: {ScoreTools.Average(80, 90):0.0}"));
Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Average of three scores: {ScoreTools.Average(threeScores):0.0}"));

static class ScoreTools
{
    public static double Average(int first, int second) => Average(new int[] { first, second });

    public static double Average(int[] scores)
    {
        int total = 0;
        for (int index = 0; index < scores.Length; index++)
        {
            total += scores[index];
        }

        return (double)total / scores.Length;
    }
}
