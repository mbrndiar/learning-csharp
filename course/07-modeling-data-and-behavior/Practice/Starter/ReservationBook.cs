namespace ModelingDataBehaviorPractice;

public sealed class ReservationBook
{
    public IReadOnlyList<Reservation> Reservations => throw new NotImplementedException("Expose a read-only view of the stored reservations.");

    public void Add(Reservation reservation) => throw new NotImplementedException("Store a reservation.");

    public IReadOnlyList<Reservation> FindByGuest(string email) =>
        throw new NotImplementedException("Find reservations by guest email.");

    public int CountConfirmedOn(DateOnly day) => throw new NotImplementedException("Count only confirmed reservations on the given day.");
}
