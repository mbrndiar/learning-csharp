using System.Collections.Immutable;

namespace ComparativeKv.Core;

public sealed record SetResult(Entry Entry, bool Created);
