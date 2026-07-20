using ComparativeKv.Tests.Support;

namespace ComparativeKv.Tests.Domain;

public sealed class MilestoneOneFixtureTests
{
    [Fact]
    public async Task KeyFixturesPassAtTheProcessBoundary()
    {
        await FixtureRunner.RunKeyFixtureAsync();
    }

    [Fact]
    public async Task AcceptedValueFixturesPassAtTheProcessBoundary()
    {
        await FixtureRunner.RunAcceptedValueFixtureAsync();
    }

    [Fact]
    public async Task RejectedValueFixturesPassWithoutCreatingStorage()
    {
        await FixtureRunner.RunRejectedValueFixtureAsync();
    }
}
