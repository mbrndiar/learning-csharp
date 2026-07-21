namespace ProjectsSolutionsBuildsPractice;

public static class BuildLayout
{
    // TODO: Implement GetOutputDirectory. Reject a missing configuration or target framework, then return the exact bin/<Configuration>/<TargetFramework>/ path.
    public static string GetOutputDirectory(string configuration, string targetFramework) =>
        throw new NotImplementedException("Return bin/<Configuration>/<TargetFramework>/.");

    // TODO: Implement GetIntermediateDirectory. Reject a missing configuration or target framework, then return the exact obj/<Configuration>/<TargetFramework>/ path.
    public static string GetIntermediateDirectory(string configuration, string targetFramework) =>
        throw new NotImplementedException("Return obj/<Configuration>/<TargetFramework>/.");

    // TODO: Implement NormalizeProjectReferences. Reject a missing sequence, leave the caller's sequence unchanged, then trim, drop blanks, de-duplicate case-insensitively, and sort the remaining references.
    public static IReadOnlyList<string> NormalizeProjectReferences(IEnumerable<string> projectReferences) =>
        throw new NotImplementedException("Trim, de-duplicate, and sort the references.");

    // TODO: Implement CreateBuildSummary. Reject missing required text and missing collections without mutating either sequence, then compose the exact multi-line summary described in the README's Your task section.
    public static string CreateBuildSummary(
        string projectName,
        string configuration,
        string targetFramework,
        IEnumerable<string> sourceFiles,
        IEnumerable<string> projectReferences) =>
        throw new NotImplementedException("Compose the exact multi-line summary described in the README.");

    // TODO: Implement CreateRunCommand. Reject a missing project path or configuration, then return the dotnet run command and append --no-build only when requested.
    public static string CreateRunCommand(string projectPath, string configuration, bool noBuild) =>
        throw new NotImplementedException("Return the dotnet run command for the given project.");
}
