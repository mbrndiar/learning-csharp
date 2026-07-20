using System.Collections.ObjectModel;

namespace ComparativeKv.Core;

public enum KvExitCode
{
    Success = 0,
    Validation = 2,
    Conflict = 3,
    NotFound = 4,
    Storage = 5,
}
