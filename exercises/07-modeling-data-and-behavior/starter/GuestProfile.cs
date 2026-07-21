namespace ModelingDataBehaviorPractice;

public sealed record GuestProfile
{
    // TODO: Reject missing guest text, normalize it at the boundary, and keep the validated value immutable.
    public GuestProfile(string name, string email) => throw new NotImplementedException("Trim and validate the guest profile.");

    public string Name { get; } = string.Empty;

    public string Email { get; } = string.Empty;
}
