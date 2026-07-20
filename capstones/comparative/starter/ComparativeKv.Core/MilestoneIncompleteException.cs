using System.Collections.ObjectModel;

namespace ComparativeKv.Core;

public sealed class MilestoneIncompleteException : NotImplementedException
{
    public MilestoneIncompleteException(string feature)
        : base($"{feature} is intentionally incomplete; implement the next milestone")
    {
    }
}
