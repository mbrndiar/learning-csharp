using ComparativeKv.Tests.Support;

namespace ComparativeKv.Tests.Integration;

public sealed class MilestoneTwoCliTests
{
    [Fact]
    public async Task InvalidGrammarAndArgumentsFollowTheFrozenEnvelopeContract()
    {
        await FixtureRunner.RunSequentialFileAsync("fixtures/scenarios/invalid.json");
    }
}
