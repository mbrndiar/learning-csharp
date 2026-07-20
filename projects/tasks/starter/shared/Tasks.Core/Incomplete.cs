namespace Tasks.Core;

/// <summary>Helpers for signalling a deliberately unimplemented milestone.</summary>
public static class Incomplete
{
    /// <summary>Throw the single deliberate failure used by scaffold operations.</summary>
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    public static void Feature(string feature)
        => throw new IncompleteProjectException(feature);

    /// <summary>Throw the deliberate failure from a value-returning scaffold member.</summary>
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    public static T Value<T>(string feature)
        => throw new IncompleteProjectException(feature);
}
