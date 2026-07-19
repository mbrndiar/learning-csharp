using System.Collections.Generic;
using LearningCSharp.CollectionsAndIteration;
using Xunit;

namespace LearningCSharp.CollectionsAndIteration.Tests;

public sealed class CollectionPracticeTests
{
    [Fact]
    public void CopyWithoutBlanksReturnsTrimmedItemsInOrderAndDoesNotMutateSourceArray()
    {
        string[] source = [" apple ", "", "Pear", "  "];

        List<string> actual = CollectionPractice.CopyWithoutBlanks(source);
        actual[0] = "changed";

        Assert.Collection(actual,
            item => Assert.Equal("changed", item),
            item => Assert.Equal("Pear", item));
        Assert.Equal(" apple ", source[0]);
    }

    [Fact]
    public void CountItemsCountsNormalizedValuesAndSkipsBlanks()
    {
        List<string> items = new() { " Apple ", "apple", "PEAR", " " };

        Dictionary<string, int> actual = CollectionPractice.CountItems(items);

        Assert.Equal(2, actual["apple"]);
        Assert.Equal(1, actual["pear"]);
        Assert.Equal(2, actual.Count);
    }

    [Fact]
    public void FindDuplicatesReturnsEachNormalizedDuplicateOnlyOnce()
    {
        HashSet<string> actual = CollectionPractice.FindDuplicates([" Apple ", "pear", "apple", "PEAR", "pear"]);

        Assert.Equal(2, actual.Count);
        Assert.Contains("apple", actual);
        Assert.Contains("pear", actual);
    }

    [Fact]
    public void CollectionMethodsHandleEmptyCollections()
    {
        Assert.Empty(CollectionPractice.CopyWithoutBlanks([]));
        Assert.Empty(CollectionPractice.CountItems(new List<string>()));
        Assert.Empty(CollectionPractice.FindDuplicates([]));
    }

    [Fact]
    public void CollectionMethodsThrowArgumentNullExceptionForNullInputs()
    {
        Assert.Throws<ArgumentNullException>(() => CollectionPractice.CopyWithoutBlanks(null!));
        Assert.Throws<ArgumentNullException>(() => CollectionPractice.CountItems(null!));
        Assert.Throws<ArgumentNullException>(() => CollectionPractice.FindDuplicates(null!));
    }
}
