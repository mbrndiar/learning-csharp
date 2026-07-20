namespace ReadingProgressReport;

public static class Reports
{
    public static IEnumerable<string> GetCompletedLearners(IEnumerable<StudyRun> runs) =>
        runs.Where(static run => run.Completed)
            .OrderBy(static run => run.Learner, StringComparer.OrdinalIgnoreCase)
            .Select(static run => run.Learner);

    public static IEnumerable<StudyRun> GetTopRunsForTrack(IEnumerable<StudyRun> runs, string track, int count)
    {
        if (count <= 0)
        {
            return [];
        }

        return runs.Where(run => run.Completed && string.Equals(run.Track, track, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(static run => run.Score)
            .ThenBy(static run => run.Minutes)
            .Take(count);
    }

    public static IReadOnlyList<TrackSummary> BuildTrackSummaries(IEnumerable<StudyRun> runs) =>
        runs.Where(static run => run.Completed)
            .GroupBy(static run => run.Track, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(static group => new TrackSummary(group.First().Track, group.Count(), Math.Round(group.Average(run => run.Score), 1, MidpointRounding.AwayFromZero)))
            .ToArray();

    public static int TotalCompletedMinutes(IEnumerable<StudyRun> runs) =>
        runs.Where(static run => run.Completed).Sum(static run => run.Minutes);
}
