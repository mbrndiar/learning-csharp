using System.Collections.Immutable;

namespace ComparativeKv.Core;

public sealed record DeleteResult(string Key, long DeletedRevision, long Revision);
