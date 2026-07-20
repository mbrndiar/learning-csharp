using System.Collections.Immutable;

namespace ComparativeKv.Core;

public readonly record struct SetExpectation(ExpectationKind Kind, long? Revision)
{
    public static SetExpectation Any { get; } = new(ExpectationKind.Any, null);

    public static SetExpectation Absent { get; } = new(ExpectationKind.Absent, null);

    public static SetExpectation Exact(long revision) => new(ExpectationKind.ExactRevision, revision);
}
