using ComparativeKv.Tests.Support;

namespace ComparativeKv.Tests.Integration;

public sealed class MilestoneFourMutationTests
{
    [Fact]
    public async Task NormalMutationAndRevisionScenariosPass()
    {
        await FixtureRunner.RunSequentialFileAsync("fixtures/scenarios/normal.json");
    }

    [Fact]
    public async Task BoundaryScenariosAndReferencedFixturesPass()
    {
        await FixtureRunner.RunSequentialFileAsync("fixtures/scenarios/boundary.json");
    }
}
