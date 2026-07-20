namespace ComparativeKv.Core;

public static class KvLimits
{
    public const long MaximumSafeInteger = 9_007_199_254_740_991;
    public const int MaximumValueUtf8Bytes = 65_536;
    public const int MaximumContainerDepth = 32;
    public const int BusyTimeoutMilliseconds = 10_000;
    public const int SchemaVersion = 1;
}
