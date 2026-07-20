using System.Collections.Immutable;

namespace ComparativeKv.Core;

public enum ExpectationKind
{
    Any,
    Absent,
    ExactRevision,
}

public readonly record struct SetExpectation(ExpectationKind Kind, long? Revision)
{
    public static SetExpectation Any { get; } = new(ExpectationKind.Any, null);

    public static SetExpectation Absent { get; } = new(ExpectationKind.Absent, null);

    public static SetExpectation Exact(long revision) => new(ExpectationKind.ExactRevision, revision);
}

public readonly record struct DeleteExpectation(ExpectationKind Kind, long? Revision)
{
    public static DeleteExpectation Any { get; } = new(ExpectationKind.Any, null);

    public static DeleteExpectation Exact(long revision) => new(ExpectationKind.ExactRevision, revision);
}

public sealed record Entry(string Key, JsonValue Value, long Revision);

public sealed record SetResult(Entry Entry, bool Created);

public sealed record DeleteResult(string Key, long DeletedRevision, long Revision);

public sealed record ListResult(ImmutableArray<Entry> Entries, long GlobalRevision);
