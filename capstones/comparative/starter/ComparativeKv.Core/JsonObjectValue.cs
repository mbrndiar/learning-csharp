using System.Buffers;
using System.Collections.Immutable;
using System.Text;
using System.Text.Json;

namespace ComparativeKv.Core;

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
