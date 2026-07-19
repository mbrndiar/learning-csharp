using LearningCSharp.Course.Unit14.Practice.Domain;

namespace LearningCSharp.Course.Unit14.Practice.Application;

public sealed record SummaryReport(ReadingSummary Summary, IReadOnlyList<string> OutputLines);
