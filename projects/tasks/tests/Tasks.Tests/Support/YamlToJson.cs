using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Tasks.Tests.Support;

/// <summary>
/// Converts the block-style YAML subset used by the checked-in OpenAPI document
/// into a <see cref="JsonNode"/>. Microsoft.OpenApi 3.9.0 reads JSON, so the
/// suite converts the YAML here and then parses it semantically with the library.
/// </summary>
public static partial class YamlToJson
{
    private sealed record Line(int Indent, string Text);

    /// <summary>Convert YAML text into a JSON node tree.</summary>
    public static JsonNode Convert(string yaml)
    {
        ArgumentNullException.ThrowIfNull(yaml);
        var lines = new List<Line>();
        foreach (string raw in yaml.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            int indent = 0;
            while (indent < raw.Length && raw[indent] == ' ')
            {
                indent++;
            }

            string text = raw[indent..];
            if (text.Length == 0 || text.StartsWith('#'))
            {
                continue;
            }

            lines.Add(new Line(indent, text));
        }

        int position = 0;
        return ParseBlock(lines, ref position);
    }

    private static JsonNode ParseBlock(List<Line> lines, ref int position)
    {
        if (position >= lines.Count)
        {
            return new JsonObject();
        }

        return lines[position].Text.StartsWith('-')
            ? ParseSequence(lines, ref position, lines[position].Indent)
            : ParseMapping(lines, ref position, lines[position].Indent);
    }

    private static JsonObject ParseMapping(List<Line> lines, ref int position, int indent)
    {
        var obj = new JsonObject();
        while (position < lines.Count
               && lines[position].Indent == indent
               && !lines[position].Text.StartsWith("- ", StringComparison.Ordinal)
               && lines[position].Text != "-")
        {
            string text = lines[position].Text;
            position++;
            (string key, string value) = SplitKeyValue(text);
            key = Unquote(key);
            if (value.Length == 0)
            {
                if (position < lines.Count && lines[position].Indent > indent)
                {
                    obj[key] = ParseBlock(lines, ref position);
                }
                else
                {
                    obj[key] = null;
                }
            }
            else if (value is ">-" or ">" or "|" or "|-")
            {
                obj[key] = JsonValue.Create(ParseBlockScalar(lines, ref position, indent, value));
            }
            else
            {
                obj[key] = Scalar(value);
            }
        }

        return obj;
    }

    private static JsonArray ParseSequence(List<Line> lines, ref int position, int indent)
    {
        var arr = new JsonArray();
        while (position < lines.Count
               && lines[position].Indent == indent
               && lines[position].Text.StartsWith('-'))
        {
            string text = lines[position].Text;
            string rest = text.Length > 1 ? text[1..].TrimStart() : string.Empty;
            position++;
            if (rest.Length == 0)
            {
                arr.Add(ParseBlock(lines, ref position));
            }
            else if (LooksLikeMappingEntry(rest))
            {
                int contentIndent = indent + (text.Length - text[1..].TrimStart().Length);
                var item = new List<Line> { new(contentIndent, rest) };
                while (position < lines.Count
                       && lines[position].Indent >= contentIndent
                       && !(lines[position].Indent == indent && lines[position].Text.StartsWith('-')))
                {
                    item.Add(lines[position]);
                    position++;
                }

                int sub = 0;
                arr.Add(ParseBlock(item, ref sub));
            }
            else
            {
                arr.Add(Scalar(rest));
            }
        }

        return arr;
    }

    private static string ParseBlockScalar(List<Line> lines, ref int position, int indent, string style)
    {
        var parts = new List<string>();
        while (position < lines.Count && lines[position].Indent > indent)
        {
            parts.Add(lines[position].Text);
            position++;
        }

        return style.StartsWith('>') ? string.Join(' ', parts) : string.Join('\n', parts);
    }

    private static bool LooksLikeMappingEntry(string text)
        => MappingEntryPattern().IsMatch(text);

    private static (string Key, string Value) SplitKeyValue(string text)
    {
        int index = FindColon(text);
        if (index < 0)
        {
            return (text, string.Empty);
        }

        return (text[..index].TrimEnd(), text[(index + 1)..].Trim());
    }

    private static int FindColon(string text)
    {
        bool inDouble = false;
        bool inSingle = false;
        for (int index = 0; index < text.Length; index++)
        {
            char character = text[index];
            if (character == '"' && !inSingle)
            {
                inDouble = !inDouble;
            }
            else if (character == '\'' && !inDouble)
            {
                inSingle = !inSingle;
            }
            else if (character == ':' && !inDouble && !inSingle
                     && (index + 1 == text.Length || text[index + 1] == ' '))
            {
                return index;
            }
        }

        return -1;
    }

    private static string Unquote(string value)
    {
        if (value.Length >= 2
            && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
        {
            return value[1..^1];
        }

        return value;
    }

    private static JsonValue? Scalar(string value)
    {
        if (value.Length >= 2
            && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
        {
            return JsonValue.Create(Unquote(value));
        }

        if (string.Equals(value, "true", StringComparison.Ordinal))
        {
            return JsonValue.Create(true);
        }

        if (string.Equals(value, "false", StringComparison.Ordinal))
        {
            return JsonValue.Create(false);
        }

        if (value is "null" or "~")
        {
            return null;
        }

        if (IntegerPattern().IsMatch(value))
        {
            return JsonValue.Create(long.Parse(value, CultureInfo.InvariantCulture));
        }

        return JsonValue.Create(value);
    }

    [GeneratedRegex("^(\"[^\"]*\"|'[^']*'|[^:]+):(\\s|$)")]
    private static partial Regex MappingEntryPattern();

    [GeneratedRegex("^-?[0-9]+$")]
    private static partial Regex IntegerPattern();
}
