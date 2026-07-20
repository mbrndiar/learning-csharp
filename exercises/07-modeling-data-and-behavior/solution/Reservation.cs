namespace ModelingDataBehaviorPractice;

public sealed class Reservation
{
    public Reservation(GuestProfile guest, DateOnly day, PartySize partySize, Table table)
    {
        ArgumentNullException.ThrowIfNull(guest);
        ArgumentNullException.ThrowIfNull(table);

        if (partySize.Total <= 0)
        {
            throw new ArgumentException("A reservation requires at least one guest.", nameof(partySize));
        }

        if (!table.CanSeat(partySize))
        {
            throw new InvalidOperationException("The table is too small for the party.");
        }

        Guest = guest;
        Day = day;
        PartySize = partySize;
        Table = table;
        State = ReservationState.Draft;
    }

    public GuestProfile Guest { get; }

    public DateOnly Day { get; }

    public PartySize PartySize { get; }

    public Table Table { get; }

    public ReservationState State { get; private set; }

    public string Summary => $"{Guest.Name} on {Day:yyyy-MM-dd} for {PartySize.Total} guest(s) at table {Table.Number} [{State}]";

    public void Confirm()
    {
        if (State == ReservationState.Cancelled)
        {
            throw new InvalidOperationException("Cancelled reservations cannot be confirmed.");
        }

        State = ReservationState.Confirmed;
    }

    public void Cancel() => State = ReservationState.Cancelled;
}
