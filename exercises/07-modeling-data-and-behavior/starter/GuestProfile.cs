namespace ModelingDataBehaviorPractice;

public sealed record GuestProfile
{
    // TODO: Implement this constructor. Reject a missing name or email, then store the trimmed values as immutable, get-only properties.
    public GuestProfile(string name, string email) => throw new NotImplementedException("Trim and validate the guest profile.");

    public string Name { get; } = string.Empty;

    public string Email { get; } = string.Empty;
}
