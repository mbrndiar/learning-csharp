namespace ModelingDataBehaviorPractice;

public readonly record struct PartySize
{
    public PartySize(int adults, int children) => throw new NotImplementedException("Validate and store the guest counts.");

    public int Adults { get; }

    public int Children { get; }

    public int Total => throw new NotImplementedException("Return Adults + Children.");
}
