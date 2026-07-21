namespace AbstractionsGenericsDelegatesPractice;

public sealed record CourseCard : IKeyedItem
{
    // TODO: Validate and normalize the stable key and title before storing immutable card data.
    public CourseCard(string key, string title) => throw new NotImplementedException("Validate and store the course card data.");

    public string Key { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;
}
