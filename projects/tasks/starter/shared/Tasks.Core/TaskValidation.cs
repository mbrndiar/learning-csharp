namespace Tasks.Core;

/// <summary>
/// Pure boundary validation for task fields. The starter leaves the rules for
/// milestone one; every adapter should route untrusted values through here.
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
        // TODO(milestone 1): reject non-positive identifiers with a validation error.
        return Incomplete.Value<long>($"milestone 1 task ID validation for {value}");
    }

    /// <summary>Validate, trim, and return a task title.</summary>
    public static string ValidateTitle(string? value)
    {
        // TODO(milestone 1): trim, then enforce the length, one-line, and
        // control-character rules from the specification.
        return Incomplete.Value<string>($"milestone 1 title validation for '{value}'");
    }
}
