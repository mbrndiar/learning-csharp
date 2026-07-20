namespace ComparativeKv.Core;

public static class KeyValueValidation
{
    public static string ValidateKey(string value) => MilestoneIncomplete.Throw<string>("comparative key validation");

    public static SetExpectation ParseSetExpectation(string value) =>
        MilestoneIncomplete.Throw<SetExpectation>("comparative set expectation parsing");

    public static DeleteExpectation ParseDeleteExpectation(string value) =>
        MilestoneIncomplete.Throw<DeleteExpectation>("comparative delete expectation parsing");

    public static long ParseExactRevision(string value) =>
        MilestoneIncomplete.Throw<long>("comparative exact revision parsing");
}
