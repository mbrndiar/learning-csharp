using System.Collections.Immutable;
using System.Globalization;
using System.Text;

namespace ComparativeKv.Core;

public static class RestrictedJson
{
    private static readonly Encoding InputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);

    public static JsonValue Parse(string text) => ParseCore(text, requireNormalized: false);

    public static JsonValue ParseStored(string text) => ParseCore(text, requireNormalized: true);

    private static JsonValue ParseCore(string text, bool requireNormalized)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (InputEncoding.GetByteCount(text) > KvLimits.MaximumValueUtf8Bytes)
        {
            throw KvException.InvalidValue("byte_limit");
        }

        var parser = new Parser(text);
        var raw = parser.ParseDocument();
        var value = Normalize(raw, depth: 0, parser.Metadata);
        if (requireNormalized &&
            (parser.Metadata.HasInsignificantWhitespace ||
             parser.Metadata.HasDuplicateMember ||
             parser.Metadata.HasNonCanonicalNumber))
        {
            throw KvException.InvalidValue("not_normalized");
        }

        return value;
    }

    private static JsonValue Normalize(RawValue raw, int depth, ParseMetadata metadata) =>
        raw switch
        {
            RawNull => new JsonNullValue(),
            RawBoolean boolean => new JsonBooleanValue(boolean.Value),
            RawString text => new JsonStringValue(ValidateUnicodeScalars(text.Value)),
            RawNumber number => new JsonIntegerValue(NormalizeNumber(number.Token, metadata)),
            RawArray array => NormalizeArray(array, depth, metadata),
            RawObject obj => NormalizeObject(obj, depth, metadata),
            _ => throw KvException.InvalidJsonSyntax(),
        };

    private static JsonArrayValue NormalizeArray(RawArray array, int depth, ParseMetadata metadata)
    {
        var childDepth = NextDepth(depth);
        var items = ImmutableArray.CreateBuilder<JsonValue>(array.Items.Count);
        foreach (var item in array.Items)
        {
            items.Add(Normalize(item, childDepth, metadata));
        }

        return new JsonArrayValue(items.MoveToImmutable());
    }

    private static JsonObjectValue NormalizeObject(RawObject obj, int depth, ParseMetadata metadata)
    {
        var childDepth = NextDepth(depth);
        var lastIndexes = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var index = 0; index < obj.Members.Count; index++)
        {
            var name = obj.Members[index].Name;
            if (!lastIndexes.TryAdd(name, index))
            {
                metadata.HasDuplicateMember = true;
                lastIndexes[name] = index;
            }
        }

        var members = ImmutableArray.CreateBuilder<JsonMember>();
        for (var index = 0; index < obj.Members.Count; index++)
        {
            var member = obj.Members[index];
            if (lastIndexes[member.Name] != index)
            {
                continue;
            }

            var name = ValidateUnicodeScalars(member.Name);
            members.Add(new JsonMember(name, Normalize(member.Value, childDepth, metadata)));
        }

        return new JsonObjectValue(members.ToImmutable());
    }

    private static int NextDepth(int depth)
    {
        var next = depth + 1;
        if (next > KvLimits.MaximumContainerDepth)
        {
            throw KvException.InvalidValue("depth_limit");
        }

        return next;
    }

    private static string ValidateUnicodeScalars(string value)
    {
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (char.IsHighSurrogate(character))
            {
                if (index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1]))
                {
                    throw KvException.InvalidValue("unpaired_surrogate");
                }

                index++;
            }
            else if (char.IsLowSurrogate(character))
            {
                throw KvException.InvalidValue("unpaired_surrogate");
            }
        }

        return value;
    }

    private static long NormalizeNumber(string token, ParseMetadata metadata)
    {
        if (!double.TryParse(
                token,
                NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent,
                CultureInfo.InvariantCulture,
                out var binary64) ||
            !double.IsFinite(binary64))
        {
            throw KvException.InvalidValue("non_finite_number");
        }

        var position = 0;
        var negative = token[position] == '-';
        if (negative)
        {
            position++;
        }

        var wholeStart = position;
        while (position < token.Length && token[position] is >= '0' and <= '9')
        {
            position++;
        }

        var wholeLength = position - wholeStart;
        var fractionStart = position;
        var fractionLength = 0;
        if (position < token.Length && token[position] == '.')
        {
            position++;
            fractionStart = position;
            while (position < token.Length && token[position] is >= '0' and <= '9')
            {
                position++;
            }

            fractionLength = position - fractionStart;
        }

        var exponent = 0;
        if (position < token.Length)
        {
            position++;
            var exponentNegative = false;
            if (token[position] is '+' or '-')
            {
                exponentNegative = token[position] == '-';
                position++;
            }

            exponent = ParseBoundedExponent(token.AsSpan(position), exponentNegative);
        }

        var digits = fractionLength == 0
            ? token.AsSpan(wholeStart, wholeLength).ToString()
            : string.Concat(
                token.AsSpan(wholeStart, wholeLength),
                token.AsSpan(fractionStart, fractionLength));
        if (digits.All(static character => character == '0'))
        {
            if (token != "0")
            {
                metadata.HasNonCanonicalNumber = true;
            }

            return 0;
        }

        var scale = fractionLength - exponent;
        string magnitude;
        if (scale <= 0)
        {
            var significant = digits.TrimStart('0');
            var zeroCount = -scale;
            if (significant.Length + zeroCount > 16)
            {
                throw KvException.InvalidValue("number_out_of_range");
            }

            magnitude = significant + new string('0', zeroCount);
        }
        else
        {
            if (scale >= digits.Length)
            {
                throw KvException.InvalidValue("non_integral_number");
            }

            foreach (var character in digits.AsSpan(digits.Length - scale))
            {
                if (character != '0')
                {
                    throw KvException.InvalidValue("non_integral_number");
                }
            }

            magnitude = digits.Substring(0, digits.Length - scale).TrimStart('0');
        }

        if (magnitude.Length > 16 ||
            (magnitude.Length == 16 && string.CompareOrdinal(magnitude, "9007199254740991") > 0))
        {
            throw KvException.InvalidValue("number_out_of_range");
        }

        var integer = long.Parse(magnitude, NumberStyles.None, CultureInfo.InvariantCulture);
        var result = negative ? -integer : integer;
        if (!string.Equals(token, result.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal))
        {
            metadata.HasNonCanonicalNumber = true;
        }

        return result;
    }

    private static int ParseBoundedExponent(ReadOnlySpan<char> text, bool negative)
    {
        var limit = KvLimits.MaximumValueUtf8Bytes + 16;
        var value = 0;
        foreach (var character in text)
        {
            value = Math.Min(limit, (value * 10) + (character - '0'));
        }

        return negative ? -value : value;
    }

    private abstract record RawValue;

    private sealed record RawNull : RawValue;

    private sealed record RawBoolean(bool Value) : RawValue;

    private sealed record RawString(string Value) : RawValue;

    private sealed record RawNumber(string Token) : RawValue;

    private sealed record RawArray(List<RawValue> Items) : RawValue;

    private sealed record RawMember(string Name, RawValue Value);

    private sealed record RawObject(List<RawMember> Members) : RawValue;

    private sealed class ParseMetadata
    {
        public bool HasInsignificantWhitespace { get; set; }

        public bool HasDuplicateMember { get; set; }

        public bool HasNonCanonicalNumber { get; set; }
    }

    private sealed class Parser
    {
        private readonly string text;
        private int index;

        public Parser(string text)
        {
            this.text = text;
            Metadata = new ParseMetadata();
        }

        public ParseMetadata Metadata { get; }

        public RawValue ParseDocument()
        {
            SkipWhitespace();
            var value = ParseValue();
            SkipWhitespace();
            if (index != text.Length)
            {
                throw KvException.InvalidJsonSyntax();
            }

            return value;
        }

        private RawValue ParseValue()
        {
            if (index >= text.Length)
            {
                throw KvException.InvalidJsonSyntax();
            }

            return text[index] switch
            {
                'n' => ParseKeyword("null", new RawNull()),
                't' => ParseKeyword("true", new RawBoolean(true)),
                'f' => ParseKeyword("false", new RawBoolean(false)),
                '"' => new RawString(ParseString()),
                '[' => ParseArray(),
                '{' => ParseObject(),
                '-' or >= '0' and <= '9' => ParseNumber(),
                _ => throw KvException.InvalidJsonSyntax(),
            };
        }

        private RawValue ParseKeyword(string keyword, RawValue value)
        {
            if (!text.AsSpan(index).StartsWith(keyword, StringComparison.Ordinal))
            {
                throw KvException.InvalidJsonSyntax();
            }

            index += keyword.Length;
            return value;
        }

        private RawArray ParseArray()
        {
            index++;
            SkipWhitespace();
            var items = new List<RawValue>();
            if (TryConsume(']'))
            {
                return new RawArray(items);
            }

            while (true)
            {
                items.Add(ParseValue());
                SkipWhitespace();
                if (TryConsume(']'))
                {
                    return new RawArray(items);
                }

                Require(',');
                SkipWhitespace();
                if (index >= text.Length || text[index] == ']')
                {
                    throw KvException.InvalidJsonSyntax();
                }
            }
        }

        private RawObject ParseObject()
        {
            index++;
            SkipWhitespace();
            var members = new List<RawMember>();
            if (TryConsume('}'))
            {
                return new RawObject(members);
            }

            while (true)
            {
                if (index >= text.Length || text[index] != '"')
                {
                    throw KvException.InvalidJsonSyntax();
                }

                var name = ParseString();
                SkipWhitespace();
                Require(':');
                SkipWhitespace();
                members.Add(new RawMember(name, ParseValue()));
                SkipWhitespace();
                if (TryConsume('}'))
                {
                    return new RawObject(members);
                }

                Require(',');
                SkipWhitespace();
                if (index >= text.Length || text[index] == '}')
                {
                    throw KvException.InvalidJsonSyntax();
                }
            }
        }

        private RawNumber ParseNumber()
        {
            var start = index;
            if (TryConsume('-') && index >= text.Length)
            {
                throw KvException.InvalidJsonSyntax();
            }

            if (index >= text.Length)
            {
                throw KvException.InvalidJsonSyntax();
            }

            if (text[index] == '0')
            {
                index++;
                if (index < text.Length && text[index] is >= '0' and <= '9')
                {
                    throw KvException.InvalidJsonSyntax();
                }
            }
            else if (text[index] is >= '1' and <= '9')
            {
                index++;
                while (index < text.Length && text[index] is >= '0' and <= '9')
                {
                    index++;
                }
            }
            else
            {
                throw KvException.InvalidJsonSyntax();
            }

            if (TryConsume('.'))
            {
                var fractionStart = index;
                while (index < text.Length && text[index] is >= '0' and <= '9')
                {
                    index++;
                }

                if (fractionStart == index)
                {
                    throw KvException.InvalidJsonSyntax();
                }
            }

            if (index < text.Length && text[index] is 'e' or 'E')
            {
                index++;
                if (index < text.Length && text[index] is '+' or '-')
                {
                    index++;
                }

                var exponentStart = index;
                while (index < text.Length && text[index] is >= '0' and <= '9')
                {
                    index++;
                }

                if (exponentStart == index)
                {
                    throw KvException.InvalidJsonSyntax();
                }
            }

            return new RawNumber(text.Substring(start, index - start));
        }

        private string ParseString()
        {
            Require('"');
            var builder = new StringBuilder();
            while (index < text.Length)
            {
                var character = text[index++];
                if (character == '"')
                {
                    return builder.ToString();
                }

                if (character == '\\')
                {
                    if (index >= text.Length)
                    {
                        throw KvException.InvalidJsonSyntax();
                    }

                    var escape = text[index++];
                    switch (escape)
                    {
                        case '"':
                        case '\\':
                        case '/':
                            builder.Append(escape);
                            break;
                        case 'b':
                            builder.Append('\b');
                            break;
                        case 'f':
                            builder.Append('\f');
                            break;
                        case 'n':
                            builder.Append('\n');
                            break;
                        case 'r':
                            builder.Append('\r');
                            break;
                        case 't':
                            builder.Append('\t');
                            break;
                        case 'u':
                            AppendUnicodeEscape(builder);
                            break;
                        default:
                            throw KvException.InvalidJsonSyntax();
                    }

                    continue;
                }

                if (character < ' ')
                {
                    throw KvException.InvalidJsonSyntax();
                }

                builder.Append(character);
            }

            throw KvException.InvalidJsonSyntax();
        }

        private void AppendUnicodeEscape(StringBuilder builder)
        {
            var value = ReadHexCodeUnit(index);
            index += 4;
            if (char.IsHighSurrogate((char)value) &&
                index + 6 <= text.Length &&
                text[index] == '\\' &&
                text[index + 1] == 'u')
            {
                var low = ReadHexCodeUnit(index + 2);
                if (char.IsLowSurrogate((char)low))
                {
                    builder.Append((char)value);
                    builder.Append((char)low);
                    index += 6;
                    return;
                }
            }

            builder.Append((char)value);
        }

        private int ReadHexCodeUnit(int start)
        {
            if (start + 4 > text.Length)
            {
                throw KvException.InvalidJsonSyntax();
            }

            var value = 0;
            for (var offset = 0; offset < 4; offset++)
            {
                var character = text[start + offset];
                var digit = character switch
                {
                    >= '0' and <= '9' => character - '0',
                    >= 'a' and <= 'f' => character - 'a' + 10,
                    >= 'A' and <= 'F' => character - 'A' + 10,
                    _ => -1,
                };
                if (digit < 0)
                {
                    throw KvException.InvalidJsonSyntax();
                }

                value = (value * 16) + digit;
            }

            return value;
        }

        private void SkipWhitespace()
        {
            while (index < text.Length && text[index] is ' ' or '\t' or '\r' or '\n')
            {
                Metadata.HasInsignificantWhitespace = true;
                index++;
            }
        }

        private bool TryConsume(char expected)
        {
            if (index < text.Length && text[index] == expected)
            {
                index++;
                return true;
            }

            return false;
        }

        private void Require(char expected)
        {
            if (!TryConsume(expected))
            {
                throw KvException.InvalidJsonSyntax();
            }
        }
    }
}
