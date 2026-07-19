namespace LearningCSharp.Course.Unit12.Practice;

public static class WorkPlanLoader
{
    public static Task<IReadOnlyList<WorkItem>> LoadAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("TODO: Read the plan JSON asynchronously and return work items.");
    }
}
