using ComparativeKv.Tests.Support;

namespace ComparativeKv.Tests.Integration;

public sealed class StarterBoundaryTests
{
    [Fact]
    public async Task SelectedStarterIsExplicitlyIncompleteAndDoesNotCreateStorage()
    {
        using var scenario = new ScenarioDirectory("starter-boundary");
        using var process = ProcessRunner.StartCli(
            ["--db", scenario.DatabasePath, "list"]);
        var result = await process.WaitAsync(TimeSpan.FromSeconds(15));
        var envelope = JsonContractAssertions.AssertEnvelope(result);

        Assert.Equal(1, result.ExitCode);
        Assert.False(envelope.GetProperty("ok").GetBoolean());
        Assert.Equal("incomplete", envelope.GetProperty("error").GetProperty("category").GetString());
        Assert.False(File.Exists(scenario.DatabasePath));
    }
}
