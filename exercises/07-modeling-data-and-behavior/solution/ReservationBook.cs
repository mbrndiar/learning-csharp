namespace ModelingDataBehaviorPractice;

public sealed class ReservationBook
{
    private readonly List<Reservation> reservations = [];

    public IReadOnlyList<Reservation> Reservations => reservations.AsReadOnly();

    public void Add(Reservation reservation)
    {
        ArgumentNullException.ThrowIfNull(reservation);
        reservations.Add(reservation);
    }

    public IReadOnlyList<Reservation> FindByGuest(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email, nameof(email));
        var normalizedEmail = email.Trim();
        var matches = new List<Reservation>();

        foreach (var reservation in reservations)
        {
            if (string.Equals(reservation.Guest.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
            {
                matches.Add(reservation);
            }
        }

        return matches;
    }

    public int CountConfirmedOn(DateOnly day)
    {
        var count = 0;

        foreach (var reservation in reservations)
        {
            if (reservation.Day == day && reservation.State == ReservationState.Confirmed)
            {
                count++;
            }
        }

        return count;
    }
}
