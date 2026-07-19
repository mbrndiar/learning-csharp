namespace ReservationTracker.Domain;

public sealed record GuestProfile
{
    public GuestProfile(string name, string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentException.ThrowIfNullOrWhiteSpace(email, nameof(email));

        Name = name.Trim();
        Email = email.Trim();
    }

    public string Name { get; }

    public string Email { get; }
}
