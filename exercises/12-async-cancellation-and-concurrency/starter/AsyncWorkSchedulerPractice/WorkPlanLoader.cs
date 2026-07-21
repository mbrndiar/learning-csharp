namespace LearningCSharp.Course.Unit12.Practice;

public static class WorkPlanLoader
{
    public static Task<IReadOnlyList<WorkItem>> LoadAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        // TODO: Reject a blank path.
        // TODO: Read the file with async I/O and honor the cancellation token while deserializing.
        // TODO: Surface malformed or missing content as InvalidDataException.
        throw new NotImplementedException("TODO: Read the plan JSON asynchronously and return work items.");
    }
}
