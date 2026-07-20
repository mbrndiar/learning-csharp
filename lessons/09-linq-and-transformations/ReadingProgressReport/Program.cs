using ReadingProgressReport;

var runs = new[]
{
    new StudyRun("Ada", "Backend", 95, 60, true),
    new StudyRun("Grace", "Web", 88, 45, true),
    new StudyRun("Linus", "Backend", 93, 60, true),
    new StudyRun("Mika", "Backend", 72, 30, false),
};

Console.WriteLine($"Completed learners: {string.Join(", ", Reports.GetCompletedLearners(runs))}");
Console.WriteLine($"Backend sessions: {Reports.GetTopRunsForTrack(runs, "Backend", 5).Count()}");
Console.WriteLine("Track summaries:");
foreach (var summary in Reports.BuildTrackSummaries(runs))
{
    Console.WriteLine($"- {summary.Track}: {summary.CompletedCount} completed, average score {summary.AverageScore:F1}");
}

Console.WriteLine($"Total completed minutes: {Reports.TotalCompletedMinutes(runs)}");
