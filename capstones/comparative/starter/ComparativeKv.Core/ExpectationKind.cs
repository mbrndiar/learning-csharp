using System.Collections.Immutable;

namespace ComparativeKv.Core;

public enum ExpectationKind
{
    Any,
    Absent,
    ExactRevision,
}
