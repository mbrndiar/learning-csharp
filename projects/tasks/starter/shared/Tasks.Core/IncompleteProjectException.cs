namespace Tasks.Core;

/// <summary>
/// Marks an intentionally unfinished learner milestone. The starter throws this
/// so incomplete operations fail with focused, guided feedback instead of
/// silent or misleading behavior.
/// </summary>
public sealed class IncompleteProjectException : NotImplementedException
{
    /// <summary>Create the deliberate milestone failure for one feature.</summary>
    public IncompleteProjectException(string feature)
        : base($"{feature} is intentionally incomplete; implement the matching milestone")
    {
        Feature = feature;
    }

    /// <summary>The feature description supplied by the scaffold call site.</summary>
    public string Feature { get; }
}
