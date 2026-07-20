using ProjectsSolutionsBuildsPractice;
using Xunit;

namespace ProjectsSolutionsBuildsPractice.Tests;

public sealed class BuildLayoutTests
{
    [Fact]
    public void GetOutputDirectoryUsesConfigurationAndTargetFramework()
    {
        Assert.Equal("bin/Release/net10.0/", BuildLayout.GetOutputDirectory(" Release ", " net10.0 "));
    }

    [Fact]
    public void GetIntermediateDirectoryUsesConfigurationAndTargetFramework()
    {
        Assert.Equal("obj/Debug/net10.0/", BuildLayout.GetIntermediateDirectory("Debug", "net10.0"));
    }

    [Fact]
    public void NormalizeProjectReferencesTrimsDistinctsAndSorts()
    {
        var result = BuildLayout.NormalizeProjectReferences(["  Shared.Core  ", "shared.core", "Ui", "", " Data "]);

        Assert.Equal(["Data", "Shared.Core", "Ui"], result);
    }

    [Fact]
    public void CreateBuildSummaryFormatsTheExpectedLines()
    {
        var result = BuildLayout.CreateBuildSummary(
            projectName: "KitchenSink.App",
            configuration: "Debug",
            targetFramework: "net10.0",
            sourceFiles: [" Program.cs ", "Services/GreetingService.cs", "Program.cs"],
            projectReferences: ["Shared.Text", " Shared.Core ", "shared.text"]);

        var expected = string.Join(
            Environment.NewLine,
            "Project: KitchenSink.App",
            "Assembly: KitchenSink.App.dll",
            "Configuration: Debug",
            "TargetFramework: net10.0",
            "Sources(2): Program.cs, Services/GreetingService.cs",
            "ProjectReferences(2): Shared.Core, Shared.Text",
            "Output: bin/Debug/net10.0/",
            "Intermediate: obj/Debug/net10.0/");

        Assert.Equal(expected, result);
    }

    [Fact]
    public void CreateBuildSummaryUsesNoneWhenNoReferencesExist()
    {
        var result = BuildLayout.CreateBuildSummary(
            projectName: "Solo.App",
            configuration: "Debug",
            targetFramework: "net10.0",
            sourceFiles: ["Program.cs"],
            projectReferences: []);

        Assert.Contains("ProjectReferences(0): (none)", result, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RequiredStringInputsRejectMissingText(string? text)
    {
        Assert.ThrowsAny<ArgumentException>(() => BuildLayout.GetOutputDirectory(text!, "net10.0"));
        Assert.ThrowsAny<ArgumentException>(() => BuildLayout.GetIntermediateDirectory("Debug", text!));
        Assert.ThrowsAny<ArgumentException>(() => BuildLayout.CreateRunCommand(text!, "Debug", noBuild: false));
    }

    [Fact]
    public void CreateBuildSummaryRejectsNullCollections()
    {
        Assert.Throws<ArgumentNullException>(() => BuildLayout.CreateBuildSummary("App", "Debug", "net10.0", null!, []));
        Assert.Throws<ArgumentNullException>(() => BuildLayout.CreateBuildSummary("App", "Debug", "net10.0", [], null!));
    }

    [Fact]
    public void CreateRunCommandAppendsNoBuildOnlyWhenRequested()
    {
        const string projectPath = "lessons/06_projects_solutions_and_builds/project_workbench/ProjectWorkbench.App/ProjectWorkbench.App.csproj";
        var withNoBuild = BuildLayout.CreateRunCommand(projectPath, "Debug", noBuild: true);
        var normal = BuildLayout.CreateRunCommand(projectPath, "Debug", noBuild: false);

        Assert.Equal($"dotnet run --project {projectPath} --configuration Debug --no-build", withNoBuild);
        Assert.Equal($"dotnet run --project {projectPath} --configuration Debug", normal);
    }
}
