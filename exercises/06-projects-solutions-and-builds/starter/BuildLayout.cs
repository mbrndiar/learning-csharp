namespace ProjectsSolutionsBuildsPractice;

public static class BuildLayout
{
    // TODO: Validate and normalize both required segments before describing the deterministic output location.
    public static string GetOutputDirectory(string configuration, string targetFramework) =>
        throw new NotImplementedException("Return bin/<Configuration>/<TargetFramework>/.");

    // TODO: Validate and normalize both required segments before describing the deterministic intermediate location.
    public static string GetIntermediateDirectory(string configuration, string targetFramework) =>
        throw new NotImplementedException("Return obj/<Configuration>/<TargetFramework>/.");

    // TODO: Reject a missing sequence while preserving caller ownership, then normalize non-blank references deterministically.
    public static IReadOnlyList<string> NormalizeProjectReferences(IEnumerable<string> projectReferences) =>
        throw new NotImplementedException("Trim, de-duplicate, and sort the references.");

    // TODO: Validate required inputs without mutating either sequence, then compose the documented deterministic summary.
    public static string CreateBuildSummary(
        string projectName,
        string configuration,
        string targetFramework,
        IEnumerable<string> sourceFiles,
        IEnumerable<string> projectReferences) =>
        throw new NotImplementedException("Compose the exact multi-line summary described in the README.");

    // TODO: Validate the required command inputs and let the no-build choice control only the optional command behavior.
    public static string CreateRunCommand(string projectPath, string configuration, bool noBuild) =>
        throw new NotImplementedException("Return the dotnet run command for the given project.");
}
