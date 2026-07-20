namespace ComparativeKv.Core;

public static class RestrictedJson
{
    public static JsonValue Parse(string text) =>
        MilestoneIncomplete.Throw<JsonValue>("comparative restricted JSON parsing");

    public static JsonValue ParseStored(string text) =>
        MilestoneIncomplete.Throw<JsonValue>("comparative normalized stored JSON parsing");
}
