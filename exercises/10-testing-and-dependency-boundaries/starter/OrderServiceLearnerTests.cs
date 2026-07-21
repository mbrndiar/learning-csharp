using Xunit;

namespace TestingDependencyBoundariesPractice.Tests;

// Write your own tests for OrderService below. OrderServiceLearnerTestRequirements only
// checks the *shape* of this class -- one enabled [Fact] plus one enabled [Theory] with
// at least two [InlineData] rows -- it does not check which scenario, fake, or assertion
// you choose. Prefer arrange-act-assert and small hand-written fakes for
// IInventoryGateway and IReceiptStore (no mocking package) where they help keep a test
// focused on one observable OrderService behavior.
public sealed class OrderServiceLearnerTests
{
    // TODO: Replace AddFactScenario with an enabled [Fact] that exercises one observable
    // OrderService behavior. Choose the scenario and assertion yourself.
    public void AddFactScenario() =>
        throw new NotImplementedException("Replace this scaffold with an enabled [Fact] test.");

    // TODO: Replace AddTheoryScenarios with an enabled [Theory] covering at least two
    // [InlineData] rows for a boundary or failure behavior. Choose the rows and assertion yourself.
    public void AddTheoryScenarios(int value) =>
        throw new NotImplementedException("Replace this scaffold with an enabled [Theory] test and multiple [InlineData] rows.");
}
