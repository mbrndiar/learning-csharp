namespace ProjectWorkbench.App;

internal static class BuildMetadata
{
#if DEBUG
    public const string Configuration = "Debug";
#else
    public const string Configuration = "Release";
#endif
}
