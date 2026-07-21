namespace LearningCSharp.Course.Unit12.Practice;

public static class WorkPlanLoader
{
    public static Task<IReadOnlyList<WorkItem>> LoadAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement LoadAsync so it rejects a blank path, reads the file with
        // async I/O while honoring the cancellation token during deserialization,
        // and surfaces malformed or missing content as InvalidDataException.
        throw new NotImplementedException("TODO: Read the plan JSON asynchronously and return work items.");
    }
}
