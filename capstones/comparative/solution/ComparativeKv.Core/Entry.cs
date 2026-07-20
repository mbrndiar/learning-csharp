using System.Collections.Immutable;

namespace ComparativeKv.Core;

public sealed record Entry(string Key, JsonValue Value, long Revision);
