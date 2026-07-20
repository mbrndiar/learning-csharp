namespace AbstractionsGenericsDelegatesPractice;

public sealed record CourseCard : IKeyedItem
{
    public CourseCard(string key, string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));

        Key = key.Trim();
        Title = title.Trim();
    }

    public string Key { get; init; }

    public string Title { get; init; }
}
