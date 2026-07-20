using System.Collections.Immutable;

namespace ComparativeKv.Core;

public readonly record struct DeleteExpectation(ExpectationKind Kind, long? Revision)
{
    public static DeleteExpectation Any { get; } = new(ExpectationKind.Any, null);

    public static DeleteExpectation Exact(long revision) => new(ExpectationKind.ExactRevision, revision);
}
