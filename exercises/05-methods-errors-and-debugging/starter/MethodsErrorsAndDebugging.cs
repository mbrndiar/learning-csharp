namespace LearningCSharp.MethodsErrorsAndDebugging;

public static class ScoreCalculator
{
    public static double Average(int first, int second)
    {
        // TODO: Implement Average(int, int) to delegate to Average(int[]) so both overloads share
        // the same validation and averaging behavior instead of duplicating it.
        throw new NotImplementedException("TODO: average two scores.");
    }

    public static double Average(int[] scores)
    {
        // TODO: Implement Average(int[]) to throw an ArgumentNullException for a null array, an
        // ArgumentException (with ParamName "scores") for an empty array, and an
        // ArgumentOutOfRangeException for any score outside 0 through 100.
        // TODO: Implement Average(int[]) to return the fractional (non-truncated) average of the
        // scores.
        throw new NotImplementedException("TODO: average an array of scores.");
    }

    public static string DescribeAverage(int[] scores)
    {
        // TODO: Implement DescribeAverage to reuse Average(int[]) and return "needs work" below
        // 50, "pass" from 50 to below 70, "good" from 70 to below 90, and "excellent" from 90.
        throw new NotImplementedException("TODO: describe the average score.");
    }
}
