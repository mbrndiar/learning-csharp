namespace ModelingDataBehaviorPractice;

public sealed class Reservation
{
    // TODO: Validate composed values, reject an unusable party or table, and establish the initial lifecycle state.
    public Reservation(GuestProfile guest, DateOnly day, PartySize partySize, Table table) =>
        throw new NotImplementedException("Compose the reservation and protect its invariants.");

    public GuestProfile Guest { get; } = null!;

    public DateOnly Day { get; }

    public PartySize PartySize { get; }

    public Table Table { get; } = null!;

    public ReservationState State { get; private set; }

    // TODO: Present a readable view of the reservation's composed data and current state without changing either.
    public string Summary => throw new NotImplementedException("Return a readable reservation summary.");

    // TODO: Allow only the valid confirmation transition and reject attempts that violate the cancellation boundary.
    public void Confirm() => throw new NotImplementedException("Confirm a draft reservation.");

    // TODO: Move the reservation into its cancelled state while keeping lifecycle mutation inside this type.
    public void Cancel() => throw new NotImplementedException("Cancel the reservation.");
}
