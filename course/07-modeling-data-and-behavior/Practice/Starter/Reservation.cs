namespace ModelingDataBehaviorPractice;

public sealed class Reservation
{
    public Reservation(GuestProfile guest, DateOnly day, PartySize partySize, Table table) =>
        throw new NotImplementedException("Compose the reservation and protect its invariants.");

    public GuestProfile Guest { get; } = null!;

    public DateOnly Day { get; }

    public PartySize PartySize { get; }

    public Table Table { get; } = null!;

    public ReservationState State { get; private set; }

    public string Summary => throw new NotImplementedException("Return a readable reservation summary.");

    public void Confirm() => throw new NotImplementedException("Confirm a draft reservation.");

    public void Cancel() => throw new NotImplementedException("Cancel the reservation.");
}
