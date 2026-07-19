namespace LinqTransformationsPractice;

public static class CourseRunReports
{
    public static IEnumerable<string> GetCompletedLearners(IEnumerable<CourseRun> runs)
    {
        ArgumentNullException.ThrowIfNull(runs);

        return runs.Where(static run => run.Completed)
            .OrderBy(static run => run.Learner, StringComparer.OrdinalIgnoreCase)
            .Select(static run => run.Learner);
    }

    public static IEnumerable<CourseRun> GetTopRunsForTrack(IEnumerable<CourseRun> runs, string track, int count)
    {
        ArgumentNullException.ThrowIfNull(runs);
        ArgumentException.ThrowIfNullOrWhiteSpace(track, nameof(track));

        if (count <= 0)
        {
            return [];
        }

        var normalizedTrack = track.Trim();
        return runs.Where(run => run.Completed && string.Equals(run.Track, normalizedTrack, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(static run => run.Score)
            .ThenBy(static run => run.Minutes)
            .Take(count);
    }

    public static IReadOnlyList<TrackSummary> BuildTrackSummaries(IEnumerable<CourseRun> runs)
    {
        ArgumentNullException.ThrowIfNull(runs);

        return runs.Where(static run => run.Completed)
            .GroupBy(static run => run.Track, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(static group => new TrackSummary(group.First().Track, group.Count(), Math.Round(group.Average(run => run.Score), 1, MidpointRounding.AwayFromZero)))
            .ToArray();
    }

    public static int TotalCompletedMinutes(IEnumerable<CourseRun> runs)
    {
        ArgumentNullException.ThrowIfNull(runs);
        return runs.Where(static run => run.Completed).Sum(static run => run.Minutes);
    }
}
