namespace ModelingDataBehaviorPractice;

public sealed class Table
{
    public Table(int number, int seats) => throw new NotImplementedException("Validate and store the table details.");

    public int Number { get; }

    public int Seats { get; }

    public bool CanSeat(PartySize partySize) => throw new NotImplementedException("Return whether the table can seat the party.");
}
