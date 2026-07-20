using System.Collections.Immutable;
using ComparativeKv.Core;

namespace ComparativeKv.Tests.Domain;

public sealed class JsonValueSemanticTests
{
    [Fact]
    public void SemanticEqualityHandlesEveryScalarAndArrayShape()
    {
        Assert.True(JsonValue.SemanticallyEquals(new JsonNullValue(), new JsonNullValue()));
        Assert.True(JsonValue.SemanticallyEquals(new JsonBooleanValue(true), new JsonBooleanValue(true)));
        Assert.False(JsonValue.SemanticallyEquals(new JsonBooleanValue(true), new JsonBooleanValue(false)));
        Assert.True(JsonValue.SemanticallyEquals(new JsonIntegerValue(7), new JsonIntegerValue(7)));
        Assert.False(JsonValue.SemanticallyEquals(new JsonIntegerValue(7), new JsonIntegerValue(8)));
        Assert.True(JsonValue.SemanticallyEquals(new JsonStringValue("same"), new JsonStringValue("same")));
        Assert.False(JsonValue.SemanticallyEquals(new JsonStringValue("same"), new JsonStringValue("different")));
        Assert.False(JsonValue.SemanticallyEquals(new JsonStringValue("7"), new JsonIntegerValue(7)));

        var nested = Array(new JsonIntegerValue(2));
        Assert.True(JsonValue.SemanticallyEquals(
            Array(new JsonNullValue(), nested),
            Array(new JsonNullValue(), Array(new JsonIntegerValue(2)))));
        Assert.False(JsonValue.SemanticallyEquals(
            Array(new JsonIntegerValue(1)),
            Array(new JsonIntegerValue(1), new JsonIntegerValue(2))));
        Assert.False(JsonValue.SemanticallyEquals(
            Array(new JsonIntegerValue(1)),
            Array(new JsonIntegerValue(2))));
    }

    [Fact]
    public void SemanticEqualityTreatsObjectMembersAsAnUnorderedSet()
    {
        var left = Object(
            new JsonMember("a", new JsonIntegerValue(1)),
            new JsonMember("b", new JsonBooleanValue(true)));
        var reordered = Object(
            new JsonMember("b", new JsonBooleanValue(true)),
            new JsonMember("a", new JsonIntegerValue(1)));

        Assert.True(JsonValue.SemanticallyEquals(left, reordered));
        Assert.False(JsonValue.SemanticallyEquals(left, Object(new JsonMember("a", new JsonIntegerValue(1)))));
        Assert.False(JsonValue.SemanticallyEquals(
            left,
            Object(
                new JsonMember("a", new JsonIntegerValue(2)),
                new JsonMember("b", new JsonBooleanValue(true)))));
        Assert.False(JsonValue.SemanticallyEquals(
            Object(
                new JsonMember("a", new JsonIntegerValue(1)),
                new JsonMember("b", new JsonIntegerValue(2))),
            Object(
                new JsonMember("a", new JsonIntegerValue(1)),
                new JsonMember("a", new JsonIntegerValue(2)))));
    }

    [Fact]
    public void SemanticEqualityRejectsNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => JsonValue.SemanticallyEquals(null!, new JsonNullValue()));
        Assert.Throws<ArgumentNullException>(() => JsonValue.SemanticallyEquals(new JsonNullValue(), null!));
    }

    private static JsonArrayValue Array(params JsonValue[] items) =>
        new(ImmutableArray.Create(items));

    private static JsonObjectValue Object(params JsonMember[] members) =>
        new(ImmutableArray.Create(members));
}
