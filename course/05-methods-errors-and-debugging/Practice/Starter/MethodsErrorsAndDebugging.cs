namespace LearningCSharp.MethodsErrorsAndDebugging;

public static class ScoreCalculator
{
    public static double Average(int first, int second)
    {
        // TODO: Reuse the array overload so the averaging logic lives in one place.
        throw new NotImplementedException("TODO: average two scores.");
    }

    public static double Average(int[] scores)
    {
        // TODO: Throw for null, empty arrays, or scores outside 0 through 100.
        // TODO: Return a fractional average.
        throw new NotImplementedException("TODO: average an array of scores.");
    }

    public static string DescribeAverage(int[] scores)
    {
        // TODO: Reuse Average and map the result to excellent, good, pass, or needs work.
        throw new NotImplementedException("TODO: describe the average score.");
    }
}
