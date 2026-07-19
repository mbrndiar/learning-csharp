using LinqTransformationsPractice;
using Xunit;

namespace LinqTransformationsPractice.Tests;

public sealed class CourseRunReportsTests
{
    [Fact]
    public void GetCompletedLearnersReturnsAlphabeticalNames()
    {
        var runs = CreateRuns();

        var names = CourseRunReports.GetCompletedLearners(runs).ToArray();

        Assert.Equal(["Ada", "Grace", "Linus"], names);
    }

    [Fact]
    public void GetCompletedLearnersIsDeferred()
    {
        var runs = new List<CourseRun>
        {
            new("Ada", "Backend", 95, 60, true),
        };

        var query = CourseRunReports.GetCompletedLearners(runs);
        runs.Add(new CourseRun("Grace", "Web", 88, 45, true));

        Assert.Equal(["Ada", "Grace"], query.ToArray());
    }

    [Fact]
    public void GetTopRunsForTrackOrdersByScoreThenMinutes()
    {
        var runs = CreateRuns();

        var topRuns = CourseRunReports.GetTopRunsForTrack(runs, "backend", 2).ToArray();

        Assert.Equal([95, 93], topRuns.Select(static run => run.Score).ToArray());
        Assert.Equal(["Ada", "Linus"], topRuns.Select(static run => run.Learner).ToArray());
    }

    [Fact]
    public void GetTopRunsForTrackReturnsEmptyWhenCountIsNotPositive()
    {
        var runs = CreateRuns();

        Assert.Empty(CourseRunReports.GetTopRunsForTrack(runs, "Backend", 0));
    }

    [Fact]
    public void BuildTrackSummariesMaterializesASnapshot()
    {
        var runs = new List<CourseRun>
        {
            new("Ada", "Backend", 95, 60, true),
            new("Grace", "Web", 88, 45, true),
        };

        var summaries = CourseRunReports.BuildTrackSummaries(runs);
        runs.Add(new CourseRun("Linus", "Backend", 93, 60, true));

        Assert.Equal(2, summaries.Count);
        Assert.Equal(1, summaries.Single(summary => summary.Track == "Backend").CompletedCount);
    }

    [Fact]
    public void BuildTrackSummariesGroupsAndAveragesCompletedRuns()
    {
        var summaries = CourseRunReports.BuildTrackSummaries(CreateRuns());

        Assert.Equal(2, summaries.Count);
        Assert.Equal(new TrackSummary("Backend", 2, 94.0), summaries[0]);
        Assert.Equal(new TrackSummary("Web", 1, 88.0), summaries[1]);
    }

    [Fact]
    public void TotalCompletedMinutesAddsOnlyCompletedRuns()
    {
        Assert.Equal(165, CourseRunReports.TotalCompletedMinutes(CreateRuns()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetTopRunsForTrackRejectsBlankTrack(string? track)
    {
        Assert.ThrowsAny<ArgumentException>(() => CourseRunReports.GetTopRunsForTrack(CreateRuns(), track!, 1).ToArray());
    }

    [Fact]
    public void PublicMethodsRejectNullSequences()
    {
        Assert.Throws<ArgumentNullException>(() => CourseRunReports.GetCompletedLearners(null!).ToArray());
        Assert.Throws<ArgumentNullException>(() => CourseRunReports.GetTopRunsForTrack(null!, "Backend", 1).ToArray());
        Assert.Throws<ArgumentNullException>(() => CourseRunReports.BuildTrackSummaries(null!));
        Assert.Throws<ArgumentNullException>(() => CourseRunReports.TotalCompletedMinutes(null!));
    }

    private static IReadOnlyList<CourseRun> CreateRuns() =>
    [
        new("Ada", "Backend", 95, 60, true),
        new("Grace", "Web", 88, 45, true),
        new("Linus", "Backend", 93, 60, true),
        new("Mika", "Backend", 72, 30, false),
    ];
}
