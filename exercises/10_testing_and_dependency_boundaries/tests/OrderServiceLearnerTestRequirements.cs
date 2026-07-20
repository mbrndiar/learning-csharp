using System.Reflection;
using Xunit;

namespace TestingDependencyBoundariesPractice.Tests;

public sealed class OrderServiceLearnerTestRequirements
{
    [Fact]
    public void LearnerSuiteContainsAnEnabledFact()
    {
        var hasEnabledFact = LearnerTestMethods().Any(method =>
            method.GetCustomAttribute<FactAttribute>() is { } fact &&
            method.GetCustomAttribute<TheoryAttribute>() is null &&
            IsEnabled(fact));

        Assert.True(
            hasEnabledFact,
            "Add an enabled [Fact] to OrderServiceLearnerTests for one observable OrderService behavior. The supplied check intentionally does not prescribe the scenario or assertion.");
    }

    [Fact]
    public void LearnerSuiteContainsATheoryWithMultipleRows()
    {
        var hasEnabledTheory = LearnerTestMethods().Any(method =>
            method.GetCustomAttribute<TheoryAttribute>() is { } theory &&
            IsEnabled(theory) &&
            method.GetCustomAttributes(typeof(InlineDataAttribute), inherit: false).Length >= 2);

        Assert.True(
            hasEnabledTheory,
            "Add an enabled [Theory] to OrderServiceLearnerTests with at least two [InlineData] rows for a boundary or failure behavior. The supplied check intentionally does not prescribe the rows or assertions.");
    }

    private static MethodInfo[] LearnerTestMethods() =>
        typeof(OrderServiceLearnerTestRequirements).Assembly
            .GetType($"{typeof(OrderServiceLearnerTestRequirements).Namespace}.OrderServiceLearnerTests")
            ?.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
        ?? [];

    private static bool IsEnabled(Attribute attribute) =>
        attribute.GetType().GetProperty("Skip")?.GetValue(attribute) is not string skip ||
        string.IsNullOrWhiteSpace(skip);
}
