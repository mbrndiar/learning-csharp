namespace LinqTransformationsPractice;

public static class CourseRunReports
{
    public static IEnumerable<string> GetCompletedLearners(IEnumerable<CourseRun> runs) =>
        throw new NotImplementedException("Return a deferred sequence of completed learner names.");

    public static IEnumerable<CourseRun> GetTopRunsForTrack(IEnumerable<CourseRun> runs, string track, int count) =>
        throw new NotImplementedException("Return the deferred top runs for the chosen track.");

    public static IReadOnlyList<TrackSummary> BuildTrackSummaries(IEnumerable<CourseRun> runs) =>
        throw new NotImplementedException("Materialize grouped summaries for completed runs.");

    public static int TotalCompletedMinutes(IEnumerable<CourseRun> runs) =>
        throw new NotImplementedException("Aggregate the completed minutes.");
}
