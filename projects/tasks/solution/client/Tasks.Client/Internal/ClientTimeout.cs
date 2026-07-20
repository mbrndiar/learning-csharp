namespace Tasks.Client;

internal static class ClientTimeout
{
    public static void Validate(TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero || timeout == System.Threading.Timeout.InfiniteTimeSpan)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), "timeout must be positive and finite");
        }
    }
}
