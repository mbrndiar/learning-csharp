namespace ModelingDataBehaviorPractice;

public sealed record GuestProfile
{
    public GuestProfile(string name, string email) => throw new NotImplementedException("Trim and validate the guest profile.");

    public string Name { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;
}
