namespace AbstractionsGenericsDelegatesPractice;

public sealed record CourseCard : IKeyedItem
{
    // TODO: Implement this constructor. Reject a missing key or title, then store the trimmed values as immutable card data.
    public CourseCard(string key, string title) => throw new NotImplementedException("Validate and store the course card data.");

    public string Key { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;
}
