using System.Buffers;
using System.Collections.Immutable;
using System.Text;
using System.Text.Json;

namespace ComparativeKv.Core;

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
