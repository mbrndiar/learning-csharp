namespace ModelingDataBehaviorPractice;

public sealed class Table
{
    // TODO: Reject invalid table details before storing the immutable capacity data.
    public Table(int number, int seats) => throw new NotImplementedException("Validate and store the table details.");

    public int Number { get; }

    public int Seats { get; }

    // TODO: Decide whether the party's total fits this table without changing either object.
    public bool CanSeat(PartySize partySize) => throw new NotImplementedException("Return whether the table can seat the party.");
}
