using System.Collections.Immutable;

namespace ComparativeKv.Core;

public sealed record ListResult(ImmutableArray<Entry> Entries, long GlobalRevision);
