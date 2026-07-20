using System.Collections.ObjectModel;

namespace ProjectsSolutionsBuildsPractice;

public static class BuildLayout
{
    public static string GetOutputDirectory(string configuration, string targetFramework)
    {
        var normalizedConfiguration = RequireText(configuration, nameof(configuration));
        var normalizedTargetFramework = RequireText(targetFramework, nameof(targetFramework));
        return $"bin/{normalizedConfiguration}/{normalizedTargetFramework}/";
    }

    public static string GetIntermediateDirectory(string configuration, string targetFramework)
    {
        var normalizedConfiguration = RequireText(configuration, nameof(configuration));
        var normalizedTargetFramework = RequireText(targetFramework, nameof(targetFramework));
        return $"obj/{normalizedConfiguration}/{normalizedTargetFramework}/";
    }

    public static IReadOnlyList<string> NormalizeProjectReferences(IEnumerable<string> projectReferences)
    {
        ArgumentNullException.ThrowIfNull(projectReferences);
        return NormalizeValues(projectReferences);
    }

    public static string CreateBuildSummary(
        string projectName,
        string configuration,
        string targetFramework,
        IEnumerable<string> sourceFiles,
        IEnumerable<string> projectReferences)
    {
        var normalizedProjectName = RequireText(projectName, nameof(projectName));
        var normalizedConfiguration = RequireText(configuration, nameof(configuration));
        var normalizedTargetFramework = RequireText(targetFramework, nameof(targetFramework));
        ArgumentNullException.ThrowIfNull(sourceFiles);
        ArgumentNullException.ThrowIfNull(projectReferences);

        var normalizedSources = NormalizeValues(sourceFiles);
        var normalizedReferences = NormalizeProjectReferences(projectReferences);
        var referenceText = normalizedReferences.Count == 0
            ? "(none)"
            : string.Join(", ", normalizedReferences);

        return string.Join(
            Environment.NewLine,
            $"Project: {normalizedProjectName}",
            $"Assembly: {normalizedProjectName}.dll",
            $"Configuration: {normalizedConfiguration}",
            $"TargetFramework: {normalizedTargetFramework}",
            $"Sources({normalizedSources.Count}): {string.Join(", ", normalizedSources)}",
            $"ProjectReferences({normalizedReferences.Count}): {referenceText}",
            $"Output: {GetOutputDirectory(normalizedConfiguration, normalizedTargetFramework)}",
            $"Intermediate: {GetIntermediateDirectory(normalizedConfiguration, normalizedTargetFramework)}");
    }

    public static string CreateRunCommand(string projectPath, string configuration, bool noBuild)
    {
        var normalizedProjectPath = RequireText(projectPath, nameof(projectPath));
        var normalizedConfiguration = RequireText(configuration, nameof(configuration));
        return noBuild
            ? $"dotnet run --project {normalizedProjectPath} --configuration {normalizedConfiguration} --no-build"
            : $"dotnet run --project {normalizedProjectPath} --configuration {normalizedConfiguration}";
    }

    private static string RequireText(string value, string paramName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value.Trim();
    }

    private static ReadOnlyCollection<string> NormalizeValues(IEnumerable<string> values)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        List<string> normalized = [];

        foreach (string value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            string trimmed = value.Trim();
            if (seen.Add(trimmed))
            {
                normalized.Add(trimmed);
            }
        }

        normalized.Sort(StringComparer.OrdinalIgnoreCase);
        return new ReadOnlyCollection<string>(normalized);
    }
}
