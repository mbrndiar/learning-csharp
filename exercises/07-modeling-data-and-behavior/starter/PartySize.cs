namespace ModelingDataBehaviorPractice;

public readonly record struct PartySize
{
    // TODO: Implement this constructor. Reject negative counts and an explicitly supplied all-zero pair, compute Total with checked arithmetic so overflow throws OverflowException, then store the values.
    public PartySize(int adults, int children) => throw new NotImplementedException("Validate and store the guest counts.");

    public int Adults { get; }

    public int Children { get; }

    public int Total { get; }
}
