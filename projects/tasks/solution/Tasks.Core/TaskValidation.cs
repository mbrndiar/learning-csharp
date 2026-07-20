using System.Globalization;

namespace Tasks.Core;

/// <summary>
/// Pure boundary validation for task fields. Every adapter routes untrusted
/// values through these helpers so persistence and HTTP layers observe the
/// same normalized, immutable domain values.
/// </summary>
public static class TaskValidation
{
    /// <summary>Minimum trimmed title length in Unicode characters.</summary>
    public const int MinTitleLength = 1;

    /// <summary>Maximum trimmed title length in Unicode characters.</summary>
    public const int MaxTitleLength = 120;

    /// <summary>Validate and return a positive task identifier.</summary>
    public static long ValidateTaskId(long value)
    {
        if (value <= 0)
        {
            throw new TaskValidationException("task ID must be a positive integer", "id");
        }

        return value;
    }

    /// <summary>Validate, trim, and return a task title.</summary>
    public static string ValidateTitle(string? value)
    {
        if (value is null)
        {
            throw new TaskValidationException("title must be a string", "title");
        }

        string title = value.Trim();
        int length = CountUnicodeCharacters(title);
        if (length < MinTitleLength || length > MaxTitleLength)
        {
            throw new TaskValidationException(
                "title must contain between 1 and 120 characters",
                "title");
        }

        if (OccupiesMultipleLines(title))
        {
            throw new TaskValidationException("title must occupy one physical line", "title");
        }

        if (ContainsControlCharacter(title))
        {
            throw new TaskValidationException("title must not contain control characters", "title");
        }

        return title;
    }

    /// <summary>
    /// Count Unicode scalar values so a surrogate pair counts as one character,
    /// matching the code-point length rule used across the project contract.
    /// </summary>
    private static int CountUnicodeCharacters(string value)
    {
        int scalarCount = 0;
        for (int index = 0; index < value.Length; scalarCount++)
        {
            index += char.IsSurrogatePair(value, index) ? 2 : 1;
        }

        return scalarCount;
    }

    private static bool OccupiesMultipleLines(string value)
    {
        foreach (char character in value)
        {
            if (character is '\n' or '\r' or '\v' or '\f'
                or '\u001c' or '\u001d' or '\u001e' or '\u0085' or '\u2028' or '\u2029')
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsControlCharacter(string value)
    {
        foreach (char character in value)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.Control)
            {
                return true;
            }
        }

        return false;
    }
}
