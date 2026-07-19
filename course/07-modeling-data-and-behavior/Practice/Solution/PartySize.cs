namespace ModelingDataBehaviorPractice;

public readonly record struct PartySize
{
    public PartySize(int adults, int children)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(adults);
        ArgumentOutOfRangeException.ThrowIfNegative(children);

        int total = checked(adults + children);
        if (total == 0)
        {
            throw new ArgumentException("A party must contain at least one guest.", nameof(adults));
        }

        Adults = adults;
        Children = children;
        Total = total;
    }

    public int Adults { get; }

    public int Children { get; }

    public int Total { get; }
}
