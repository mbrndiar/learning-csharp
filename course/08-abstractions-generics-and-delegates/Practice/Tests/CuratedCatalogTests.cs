using AbstractionsGenericsDelegatesPractice;
using Xunit;

namespace AbstractionsGenericsDelegatesPractice.Tests;

public sealed class CuratedCatalogTests
{
    [Fact]
    public void AddStoresAcceptedItemsAndAuditsThem()
    {
        var auditEntries = new List<string>();
        var catalog = new CuratedCatalog<CourseCard>(new AcceptAllRule(), auditEntries.Add);
        var card = new CourseCard("cs-basics", "C# Basics");

        catalog.Add(card);

        Assert.Equal(1, catalog.Count);
        Assert.Equal(card, catalog.FindByKey("CS-BASICS"));
        Assert.Equal(["Added cs-basics"], auditEntries);
    }

    [Fact]
    public void AddRejectsItemsThatFailTheRule()
    {
        var catalog = new CuratedCatalog<CourseCard>(new RejectKeysContainingRule("draft"));

        Assert.Throws<InvalidOperationException>(() => catalog.Add(new CourseCard("draft-card", "Draft Card")));
        Assert.Equal(0, catalog.Count);
    }

    [Fact]
    public void MapProjectsItemsInInsertionOrder()
    {
        var catalog = new CuratedCatalog<CourseCard>(new AcceptAllRule());
        catalog.Add(new CourseCard("cs-basics", "C# Basics"));
        catalog.Add(new CourseCard("linq-lab", "LINQ Lab"));

        var titles = catalog.Map(static card => card.Title);

        Assert.Equal(["C# Basics", "LINQ Lab"], titles);
    }

    [Fact]
    public void RemoveWhereUsesPredicateAndCallback()
    {
        var removedKeys = new List<string>();
        var catalog = new CuratedCatalog<CourseCard>(new AcceptAllRule());
        catalog.Add(new CourseCard("cs-basics", "C# Basics"));
        catalog.Add(new CourseCard("linq-lab", "LINQ Lab"));
        catalog.Add(new CourseCard("draft-card", "Draft Card"));

        var removed = catalog.RemoveWhere(card => card.Key.Contains("draft", StringComparison.OrdinalIgnoreCase), card => removedKeys.Add(card.Key));

        Assert.Equal(1, removed);
        Assert.Equal(["draft-card"], removedKeys);
        Assert.Equal(2, catalog.Count);
    }

    [Fact]
    public void MapCanUseALambdaThatCapturesOutsideState()
    {
        var minimumLength = 7;
        var catalog = new CuratedCatalog<CourseCard>(new AcceptAllRule());
        catalog.Add(new CourseCard("cs-basics", "C# Basics"));
        catalog.Add(new CourseCard("linq-lab", "LINQ Lab"));

        var flags = catalog.Map(card => card.Title.Length >= minimumLength);

        Assert.Equal([true, true], flags);
    }

    [Fact]
    public void ConstructorRejectsNullRule()
    {
        Assert.Throws<ArgumentNullException>(() => new CuratedCatalog<CourseCard>(null!));
    }

    [Fact]
    public void FindByKeyReturnsNullWhenMissing()
    {
        var catalog = new CuratedCatalog<CourseCard>(new AcceptAllRule());
        catalog.Add(new CourseCard("cs-basics", "C# Basics"));

        Assert.Null(catalog.FindByKey("missing"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CourseCardRejectsMissingText(string? text)
    {
        Assert.ThrowsAny<ArgumentException>(() => new CourseCard(text!, "Valid title"));
        Assert.ThrowsAny<ArgumentException>(() => new CourseCard("valid-key", text!));
    }

    private sealed class AcceptAllRule : IRule<CourseCard>
    {
        public bool Accepts(CourseCard item) => true;
    }

    private sealed class RejectKeysContainingRule(string fragment) : IRule<CourseCard>
    {
        public bool Accepts(CourseCard item) => !item.Key.Contains(fragment, StringComparison.OrdinalIgnoreCase);
    }
}
