using System.Buffers;
using System.Collections.Immutable;
using System.Text;
using System.Text.Json;

namespace ComparativeKv.Core;

public abstract record JsonValue
{
    public abstract void WriteTo(Utf8JsonWriter writer);

    public string ToCompactJson()
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            WriteTo(writer);
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    public static bool SemanticallyEquals(JsonValue left, JsonValue right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        return (left, right) switch
        {
            (JsonNullValue, JsonNullValue) => true,
            (JsonBooleanValue leftBoolean, JsonBooleanValue rightBoolean) => leftBoolean.Value == rightBoolean.Value,
            (JsonIntegerValue leftInteger, JsonIntegerValue rightInteger) => leftInteger.Value == rightInteger.Value,
            (JsonStringValue leftString, JsonStringValue rightString) => string.Equals(leftString.Value, rightString.Value, StringComparison.Ordinal),
            (JsonArrayValue leftArray, JsonArrayValue rightArray) => ArraysEqual(leftArray.Items, rightArray.Items),
            (JsonObjectValue leftObject, JsonObjectValue rightObject) => ObjectsEqual(leftObject.Members, rightObject.Members),
            _ => false,
        };
    }

    private static bool ArraysEqual(ImmutableArray<JsonValue> left, ImmutableArray<JsonValue> right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        for (var index = 0; index < left.Length; index++)
        {
            if (!SemanticallyEquals(left[index], right[index]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ObjectsEqual(ImmutableArray<JsonMember> left, ImmutableArray<JsonMember> right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        var rightByName = new Dictionary<string, JsonMember>(StringComparer.Ordinal);
        foreach (var member in right)
        {
            if (!rightByName.TryAdd(member.Name, member))
            {
                return false;
            }
        }

        var seenLeftNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var member in left)
        {
            if (!seenLeftNames.Add(member.Name) ||
                !rightByName.TryGetValue(member.Name, out var other) ||
                !SemanticallyEquals(member.Value, other.Value))
            {
                return false;
            }
        }

        return true;
    }
}

public sealed record JsonNullValue : JsonValue
{
    public override void WriteTo(Utf8JsonWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteNullValue();
    }
}

public sealed record JsonBooleanValue(bool Value) : JsonValue
{
    public override void WriteTo(Utf8JsonWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteBooleanValue(Value);
    }
}

public sealed record JsonIntegerValue(long Value) : JsonValue
{
    public override void WriteTo(Utf8JsonWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteNumberValue(Value);
    }
}

public sealed record JsonStringValue(string Value) : JsonValue
{
    public override void WriteTo(Utf8JsonWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStringValue(Value);
    }
}

public sealed record JsonArrayValue(ImmutableArray<JsonValue> Items) : JsonValue
{
    public override void WriteTo(Utf8JsonWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStartArray();
        foreach (var item in Items)
        {
            item.WriteTo(writer);
        }

        writer.WriteEndArray();
    }
}

public sealed record JsonMember(string Name, JsonValue Value);

public sealed record JsonObjectValue(ImmutableArray<JsonMember> Members) : JsonValue
{
    public override void WriteTo(Utf8JsonWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStartObject();
        foreach (var member in Members)
        {
            writer.WritePropertyName(member.Name);
            member.Value.WriteTo(writer);
        }

        writer.WriteEndObject();
    }
}
