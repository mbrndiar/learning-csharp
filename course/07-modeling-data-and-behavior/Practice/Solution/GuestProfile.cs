namespace ModelingDataBehaviorPractice;

public sealed record GuestProfile
{
    public GuestProfile(string name, string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentException.ThrowIfNullOrWhiteSpace(email, nameof(email));

        Name = name.Trim();
        Email = email.Trim();
    }

    public string Name { get; init; }

    public string Email { get; init; }
}
