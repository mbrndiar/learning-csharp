using System.Globalization;

namespace ComparativeKv.Core;

public static class KeyValueValidation
{
    public static string ValidateKey(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Length is < 1 or > 128 || !IsFirstKeyCharacter(value[0]))
        {
            throw KvException.InvalidArgument("key", "format");
        }

        foreach (var character in value)
        {
            if (character > 0x7F || !IsKeyCharacter(character))
            {
                throw KvException.InvalidArgument("key", "format");
            }
        }

        return value;
    }

    public static SetExpectation ParseSetExpectation(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return value switch
        {
            "any" => SetExpectation.Any,
            "absent" => SetExpectation.Absent,
            _ => SetExpectation.Exact(ParseExactRevision(value)),
        };
    }

    public static DeleteExpectation ParseDeleteExpectation(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return value == "any"
            ? DeleteExpectation.Any
            : DeleteExpectation.Exact(ParseExactRevision(value));
    }

    public static long ParseExactRevision(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Length == 0 || value[0] is < '1' or > '9')
        {
            throw KvException.InvalidArgument("expect", "format");
        }

        foreach (var character in value)
        {
            if (character is < '0' or > '9')
            {
                throw KvException.InvalidArgument("expect", "format");
            }
        }

        const string maximum = "9007199254740991";
        if (value.Length > maximum.Length ||
            (value.Length == maximum.Length && string.CompareOrdinal(value, maximum) > 0))
        {
            throw KvException.InvalidArgument("expect", "format");
        }

        return long.Parse(value, NumberStyles.None, CultureInfo.InvariantCulture);
    }

    private static bool IsFirstKeyCharacter(char character) =>
        (character is >= 'A' and <= 'Z') ||
        (character is >= 'a' and <= 'z') ||
        (character is >= '0' and <= '9');

    private static bool IsKeyCharacter(char character) =>
        IsFirstKeyCharacter(character) || character is '.' or '_' or '/' or '-';
}
