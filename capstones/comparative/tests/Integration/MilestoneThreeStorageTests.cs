using ComparativeKv.Tests.Support;

namespace ComparativeKv.Tests.Integration;

public sealed class MilestoneThreeStorageTests
{
    [Fact]
    public async Task StorageInitializationFixturePasses()
    {
        await FixtureRunner.RunSequentialFileAsync("fixtures/scenarios/storage.json");
    }

    [Fact]
    public async Task MigrationFixturePassesAndUsesIndependentSqliteAssertions()
    {
        await FixtureRunner.RunSequentialFileAsync("fixtures/scenarios/migration.json");
    }
}
