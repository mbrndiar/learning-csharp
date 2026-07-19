namespace ModelingDataBehaviorPractice;

public sealed class Table
{
    public Table(int number, int seats)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(number, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(seats, 1);

        Number = number;
        Seats = seats;
    }

    public int Number { get; }

    public int Seats { get; }

    public bool CanSeat(PartySize partySize) => Seats >= partySize.Total;
}
