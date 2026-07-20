using System.Buffers;
using System.Collections.Immutable;
using System.Text;
using System.Text.Json;

namespace ComparativeKv.Core;

public sealed record JsonBooleanValue(bool Value) : JsonValue
{
    public override void WriteTo(Utf8JsonWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteBooleanValue(Value);
    }
}
