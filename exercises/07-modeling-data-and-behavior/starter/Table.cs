namespace ModelingDataBehaviorPractice;

public sealed class Table
{
    // TODO: Implement this constructor. Reject a non-positive table number or seat count, then store the validated values.
    public Table(int number, int seats) => throw new NotImplementedException("Validate and store the table details.");

    public int Number { get; }

    public int Seats { get; }

    // TODO: Implement CanSeat. Return whether Seats can accommodate the party's Total guest count, without changing either object.
    public bool CanSeat(PartySize partySize) => throw new NotImplementedException("Return whether the table can seat the party.");
}
