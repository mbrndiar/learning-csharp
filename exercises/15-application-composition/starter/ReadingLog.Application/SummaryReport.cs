using LearningCSharp.Exercises.ApplicationComposition.Domain;

namespace LearningCSharp.Exercises.ApplicationComposition.Application;

public sealed record SummaryReport(ReadingSummary Summary, IReadOnlyList<string> OutputLines);
