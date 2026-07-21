namespace LinqTransformationsPractice;

public static class CourseRunReports
{
    // TODO: Validate the source and return a deferred, deterministic sequence of completed learner names.
    public static IEnumerable<string> GetCompletedLearners(IEnumerable<CourseRun> runs) =>
        throw new NotImplementedException("Return a deferred sequence of completed learner names.");

    // TODO: Validate the source and track, then return deferred, ordered results; non-positive limits produce no results.
    public static IEnumerable<CourseRun> GetTopRunsForTrack(IEnumerable<CourseRun> runs, string track, int count) =>
        throw new NotImplementedException("Return the deferred top runs for the chosen track.");

    // TODO: Validate the source, group only completed runs deterministically, and materialize a stable summary snapshot.
    public static IReadOnlyList<TrackSummary> BuildTrackSummaries(IEnumerable<CourseRun> runs) =>
        throw new NotImplementedException("Materialize grouped summaries for completed runs.");

    // TODO: Validate the source and aggregate minutes from completed runs without changing the input sequence.
    public static int TotalCompletedMinutes(IEnumerable<CourseRun> runs) =>
        throw new NotImplementedException("Aggregate the completed minutes.");
}
