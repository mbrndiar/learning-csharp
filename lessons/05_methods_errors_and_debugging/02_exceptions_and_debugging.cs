#:property NoWarn=CA1707

try
{
    PrintAverage(Array.Empty<int>());
}
catch (Exception exception)
{
    Console.WriteLine(exception.GetType().Name);
    Console.WriteLine(exception.Message);
    Console.WriteLine(exception.StackTrace is not null && exception.StackTrace.Contains(nameof(CalculateAverage)));
}

static void PrintAverage(int[] scores)
{
    Console.WriteLine(CalculateAverage(scores));
}

static double CalculateAverage(int[] scores)
{
    if (scores.Length == 0)
    {
        throw new ArgumentException("Provide at least one score.", nameof(scores));
    }

    return 100;
}
