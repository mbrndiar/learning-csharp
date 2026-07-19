namespace ProjectsSolutionsBuildsPractice;

public static class BuildLayout
{
    public static string GetOutputDirectory(string configuration, string targetFramework) =>
        throw new NotImplementedException("Return bin/<Configuration>/<TargetFramework>/.");

    public static string GetIntermediateDirectory(string configuration, string targetFramework) =>
        throw new NotImplementedException("Return obj/<Configuration>/<TargetFramework>/.");

    public static IReadOnlyList<string> NormalizeProjectReferences(IEnumerable<string> projectReferences) =>
        throw new NotImplementedException("Trim, de-duplicate, and sort the references.");

    public static string CreateBuildSummary(
        string projectName,
        string configuration,
        string targetFramework,
        IEnumerable<string> sourceFiles,
        IEnumerable<string> projectReferences) =>
        throw new NotImplementedException("Compose the exact multi-line summary described in the README.");

    public static string CreateRunCommand(string projectPath, string configuration, bool noBuild) =>
        throw new NotImplementedException("Return the dotnet run command for the given project.");
}
