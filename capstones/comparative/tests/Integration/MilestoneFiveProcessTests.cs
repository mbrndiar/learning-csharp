using ComparativeKv.Tests.Support;

namespace ComparativeKv.Tests.Integration;

public sealed class MilestoneFiveProcessTests
{
    [Fact]
    public async Task IndependentProcessRacesContentionAndCleanupPass()
    {
        await FixtureRunner.RunMultiprocessFileAsync("fixtures/scenarios/multiprocess.json");
    }
}
