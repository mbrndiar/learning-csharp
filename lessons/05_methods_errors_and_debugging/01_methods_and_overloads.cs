#:property NoWarn=CA1707

using System.Globalization;

int[] threeScores = new int[] { 70, 80, 90 };

Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Average of two scores: {ScoreTools.Average(80, 90):0.0}"));
Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Average of three scores: {ScoreTools.Average(threeScores):0.0}"));
int callerScore = 70;
int doubledCopy = ScoreTools.DoubleLocalCopy(callerScore);
var scoreHistory = new List<int> { 70, 80, 90 };
ScoreTools.RaiseFirstScore(scoreHistory);
List<int> replacement = ScoreTools.ReplaceLocalReference(scoreHistory);
Console.WriteLine($"Caller scalar remains unchanged: {callerScore}; returned copy: {doubledCopy}");
Console.WriteLine($"Caller sees list element mutation: {scoreHistory[0]}");
Console.WriteLine($"Caller keeps original list after parameter reassignment: {scoreHistory.Count}");
Console.WriteLine($"Method can return its replacement explicitly: {replacement[0]}");

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

    public static int DoubleLocalCopy(int value)
    {
        value *= 2;
        return value;
    }

    public static void RaiseFirstScore(List<int> scores) => scores[0] += 5;

    public static List<int> ReplaceLocalReference(List<int> scores)
    {
        scores = [100];
        return scores;
    }
}
