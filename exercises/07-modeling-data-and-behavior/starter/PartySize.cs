namespace ModelingDataBehaviorPractice;

public readonly record struct PartySize
{
    // TODO: Protect the value object's guest-count invariants, including invalid defaults and arithmetic overflow.
    public PartySize(int adults, int children) => throw new NotImplementedException("Validate and store the guest counts.");

    public int Adults { get; }

    public int Children { get; }

    public int Total { get; }
}
