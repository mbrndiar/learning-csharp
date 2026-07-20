using System.Globalization;
using System.Text;
using System.Text.Json;

namespace ComparativeKv.Tests.Support;

internal static class JsonContractAssertions
{
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public static JsonDocument AssertProcess(ProcessResult result, JsonElement expectation)
    {
        Assert.Equal(expectation.GetProperty("exit").GetInt32(), result.ExitCode);
        Assert.Equal(expectation.GetProperty("stderr").GetString() ?? string.Empty, result.ErrorText);

        var document = ParseSingleLineObject(result.StandardOutput);
        if (expectation.TryGetProperty("stdout", out var expectedOutput))
        {
            AssertSemanticEqual(expectedOutput, document.RootElement);
        }
        else
        {
            AssertEnvelopeShape(document.RootElement);
        }

        AssertCanonicalNumbers(document.RootElement);
        return document;
    }

    public static JsonElement AssertEnvelope(ProcessResult result)
    {
        using var document = ParseSingleLineObject(result.StandardOutput);
        Assert.Equal(string.Empty, result.ErrorText);
        AssertEnvelopeShape(document.RootElement);
        AssertCanonicalNumbers(document.RootElement);
        return document.RootElement.Clone();
    }

    public static JsonDocument ParseSingleLineObject(byte[] output)
    {
        Assert.NotEmpty(output);
        Assert.False(
            output.Length >= 3 && output[0] == 0xEF && output[1] == 0xBB && output[2] == 0xBF,
            "stdout must not have a UTF-8 BOM.");
        Assert.Equal((byte)'\n', output[^1]);
        for (var index = 0; index < output.Length - 1; index++)
        {
            Assert.True(output[index] is not (byte)'\n' and not (byte)'\r', "stdout must be one compact line.");
        }

        _ = StrictUtf8.GetString(output);
        var document = JsonDocument.Parse(output.AsMemory(0, output.Length - 1));
        Assert.Equal(JsonValueKind.Object, document.RootElement.ValueKind);
        return document;
    }

    public static void AssertSemanticEqual(JsonElement expected, JsonElement actual)
    {
        Assert.Equal(expected.ValueKind, actual.ValueKind);
        switch (expected.ValueKind)
        {
            case JsonValueKind.Object:
                {
                    var expectedMembers = Members(expected);
                    var actualMembers = Members(actual);
                    Assert.Equal(expectedMembers.Count, actualMembers.Count);
                    foreach (var (name, expectedMember) in expectedMembers)
                    {
                        Assert.True(actualMembers.TryGetValue(name, out var actualMember), $"Missing object member: {name}");
                        AssertSemanticEqual(expectedMember, actualMember);
                    }

                    break;
                }
            case JsonValueKind.Array:
                {
                    var expectedItems = expected.EnumerateArray().ToArray();
                    var actualItems = actual.EnumerateArray().ToArray();
                    Assert.Equal(expectedItems.Length, actualItems.Length);
                    for (var index = 0; index < expectedItems.Length; index++)
                    {
                        AssertSemanticEqual(expectedItems[index], actualItems[index]);
                    }

                    break;
                }
            case JsonValueKind.String:
                Assert.Equal(expected.GetString(), actual.GetString());
                break;
            case JsonValueKind.Number:
                Assert.Equal(
                    expected.GetInt64().ToString(CultureInfo.InvariantCulture),
                    actual.GetInt64().ToString(CultureInfo.InvariantCulture));
                break;
            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Null:
                break;
            default:
                throw new InvalidOperationException($"Unsupported JSON kind: {expected.ValueKind}");
        }
    }

    private static void AssertEnvelopeShape(JsonElement envelope)
    {
        var members = Members(envelope);
        Assert.Equal(2, members.Count);
        Assert.True(members.TryGetValue("ok", out var ok));
        Assert.True(ok.ValueKind is JsonValueKind.True or JsonValueKind.False);
        if (ok.GetBoolean())
        {
            Assert.True(members.ContainsKey("result"));
            return;
        }

        Assert.True(members.TryGetValue("error", out var error));
        var errorMembers = Members(error);
        Assert.Equal(2, errorMembers.Count);
        Assert.True(errorMembers.TryGetValue("category", out var category));
        Assert.Equal(JsonValueKind.String, category.ValueKind);
        Assert.True(errorMembers.TryGetValue("details", out var details));
        Assert.Equal(JsonValueKind.Object, details.ValueKind);
    }

    private static void AssertCanonicalNumbers(JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in value.EnumerateObject())
                {
                    AssertCanonicalNumbers(property.Value);
                }

                break;
            case JsonValueKind.Array:
                foreach (var item in value.EnumerateArray())
                {
                    AssertCanonicalNumbers(item);
                }

                break;
            case JsonValueKind.Number:
                {
                    var text = value.GetRawText();
                    Assert.Matches("^(0|-?[1-9][0-9]*)$", text);
                    Assert.True(long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out _));
                    break;
                }
        }
    }

    private static Dictionary<string, JsonElement> Members(JsonElement value)
    {
        Assert.Equal(JsonValueKind.Object, value.ValueKind);
        var members = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var property in value.EnumerateObject())
        {
            Assert.True(members.TryAdd(property.Name, property.Value), $"Duplicate object member: {property.Name}");
        }

        return members;
    }
}
