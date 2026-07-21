namespace LinqTransformationsPractice;

public static class CourseRunReports
{
    // TODO: Implement GetCompletedLearners. Reject a missing sequence and keep validation/filtering deferred until enumeration, then return only completed runs' learner names ordered alphabetically.
    public static IEnumerable<string> GetCompletedLearners(IEnumerable<CourseRun> runs) =>
        throw new NotImplementedException("Return a deferred sequence of completed learner names.");

    // TODO: Implement GetTopRunsForTrack. Reject a missing sequence or blank track (deferred until enumeration), then return the matching track's runs ordered by score descending then minutes ascending, limited to count, with an empty result when count is not positive.
    public static IEnumerable<CourseRun> GetTopRunsForTrack(IEnumerable<CourseRun> runs, string track, int count) =>
        throw new NotImplementedException("Return the deferred top runs for the chosen track.");

    // TODO: Implement BuildTrackSummaries. Reject a missing sequence, group only completed runs by track, then materialize a stable, track-name-ordered summary list.
    public static IReadOnlyList<TrackSummary> BuildTrackSummaries(IEnumerable<CourseRun> runs) =>
        throw new NotImplementedException("Materialize grouped summaries for completed runs.");

    // TODO: Implement TotalCompletedMinutes. Reject a missing sequence, then sum minutes from only the completed runs without changing the input sequence.
    public static int TotalCompletedMinutes(IEnumerable<CourseRun> runs) =>
        throw new NotImplementedException("Aggregate the completed minutes.");
}
