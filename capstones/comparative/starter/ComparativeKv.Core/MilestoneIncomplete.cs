using System.Collections.ObjectModel;

namespace ComparativeKv.Core;

public static class MilestoneIncomplete
{
    public static void Throw(string feature) => throw new MilestoneIncompleteException(feature);

    public static T Throw<T>(string feature) => throw new MilestoneIncompleteException(feature);
}
