namespace ModelingDataBehaviorPractice;

public sealed class Reservation
{
    // TODO: Implement this constructor. Validate the composed guest, default/unusable party size, and table; reject a too-small table; then set the initial Draft state.
    public Reservation(GuestProfile guest, DateOnly day, PartySize partySize, Table table) =>
        throw new NotImplementedException("Compose the reservation and protect its invariants.");

    public GuestProfile Guest { get; } = null!;

    public DateOnly Day { get; }

    public PartySize PartySize { get; }

    public Table Table { get; } = null!;

    public ReservationState State { get; private set; }

    // TODO: Implement Summary. Return a readable description that includes at least the guest name and the ISO calendar day, without mutating the reservation.
    public string Summary => throw new NotImplementedException("Return a readable reservation summary.");

    // TODO: Implement Confirm. Move a Draft reservation to Confirmed, and throw InvalidOperationException when confirmation is not valid (for example, already Cancelled).
    public void Confirm() => throw new NotImplementedException("Confirm a draft reservation.");

    // TODO: Implement Cancel. Move the reservation into the Cancelled state.
    public void Cancel() => throw new NotImplementedException("Cancel the reservation.");
}
