using ComparativeKv.Core;

namespace ComparativeKv.Tests.Domain;

public sealed class RestrictedJsonEdgeTests
{
    [Fact]
    public void ParserAcceptsEmptyContainersAndEveryStringEscapeForm()
    {
        var array = Assert.IsType<JsonArrayValue>(RestrictedJson.Parse("[]"));
        var obj = Assert.IsType<JsonObjectValue>(RestrictedJson.Parse("{}"));
        var text = Assert.IsType<JsonStringValue>(
            RestrictedJson.Parse("\"\\\"\\\\\\/\\b\\f\\n\\r\\t\\u0061\\u00af\\u00AF\""));

        Assert.Empty(array.Items);
        Assert.Empty(obj.Members);
        Assert.Contains('\b', text.Value);
        Assert.Contains('\f', text.Value);
        Assert.Contains('\n', text.Value);
        Assert.Contains('\r', text.Value);
        Assert.Contains('\t', text.Value);
        Assert.Contains('a', text.Value);
        Assert.Equal(2, text.Value.Count(static character => character == '¯'));
    }

    [Theory]
    [InlineData("")]
    [InlineData("nul")]
    [InlineData("[]]")]
    [InlineData("[1,]")]
    [InlineData("{")]
    [InlineData("{\"a\":1,}")]
    [InlineData("-")]
    [InlineData("01")]
    [InlineData("1.")]
    [InlineData("1e")]
    [InlineData("\"\\q\"")]
    [InlineData("\"\\u00xz\"")]
    [InlineData("\"unfinished")]
    public void ParserRejectsIncompleteOrMalformedJsonSyntax(string input)
    {
        AssertValidationError(
            () => RestrictedJson.Parse(input),
            "invalid_json",
            "syntax");
    }

    [Fact]
    public void ParserRejectsRawControlCharactersAndOutOfRangeIntegerForms()
    {
        AssertValidationError(
            () => RestrictedJson.Parse("\"\u0001\""),
            "invalid_json",
            "syntax");
        AssertValidationError(
            () => RestrictedJson.Parse("10000000000000000"),
            "invalid_value",
            "number_out_of_range");
        AssertValidationError(
            () => RestrictedJson.Parse("1e16"),
            "invalid_value",
            "number_out_of_range");
    }

    [Theory]
    [InlineData(" 1")]
    [InlineData("{\"a\":1,\"a\":1}")]
    [InlineData("1.0")]
    public void StoredValuesMustAlreadyBeNormalized(string input)
    {
        AssertValidationError(
            () => RestrictedJson.ParseStored(input),
            "invalid_value",
            "not_normalized");
    }

    [Fact]
    public void RevisionParsingRejectsNoncanonicalAndOutOfRangeText()
    {
        AssertValidationError(
            () => KeyValueValidation.ParseExactRevision("12x"),
            "invalid_argument",
            "format");
        AssertValidationError(
            () => KeyValueValidation.ParseExactRevision("9007199254740992"),
            "invalid_argument",
            "format");
        Assert.Equal(9_007_199_254_740_991, KeyValueValidation.ParseExactRevision("9007199254740991"));
    }

    private static void AssertValidationError(Action action, string category, string reason)
    {
        var exception = Assert.Throws<KvException>(action);
        Assert.Equal(category, exception.Category);
        Assert.Equal(reason, exception.Details["reason"]);
    }
}
