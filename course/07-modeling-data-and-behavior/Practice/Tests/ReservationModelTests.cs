using ModelingDataBehaviorPractice;
using Xunit;

namespace ModelingDataBehaviorPractice.Tests;

public sealed class ReservationModelTests
{
    [Fact]
    public void PartySizeTotalsGuestsAndComparesByValue()
    {
        var first = new PartySize(2, 1);
        var second = new PartySize(2, 1);

        Assert.Equal(3, first.Total);
        Assert.Equal(first, second);
    }

    [Fact]
    public void GuestProfileTrimsInputAndUsesValueEquality()
    {
        var first = new GuestProfile(" Ada ", " ada@example.com ");
        var second = new GuestProfile("Ada", "ada@example.com");

        Assert.Equal("Ada", first.Name);
        Assert.Equal(first, second);
    }

    [Fact]
    public void GuestProfilePropertiesCannotBypassConstructorValidation()
    {
        Assert.Null(typeof(GuestProfile).GetProperty(nameof(GuestProfile.Name))!.SetMethod);
        Assert.Null(typeof(GuestProfile).GetProperty(nameof(GuestProfile.Email))!.SetMethod);
    }

    [Fact]
    public void ReservationDateParsesOnlyIsoCalendarDates()
    {
        Assert.Equal(new DateOnly(2026, 7, 24), ReservationDate.ParseIso("2026-07-24"));
        Assert.Throws<FormatException>(() => ReservationDate.ParseIso("07/24/2026"));
    }

    [Fact]
    public void ReservationConfirmsAndCreatesSummary()
    {
        var reservation = new Reservation(
            new GuestProfile("Ada", "ada@example.com"),
            new DateOnly(2026, 7, 24),
            new PartySize(2, 1),
            new Table(7, 4));

        reservation.Confirm();

        Assert.Equal(ReservationState.Confirmed, reservation.State);
        Assert.Contains("Ada", reservation.Summary, StringComparison.Ordinal);
        Assert.Contains("2026-07-24", reservation.Summary, StringComparison.Ordinal);
    }

    [Fact]
    public void ReservationRejectsTablesThatAreTooSmall()
    {
        Assert.Throws<InvalidOperationException>(() => new Reservation(
            new GuestProfile("Ada", "ada@example.com"),
            new DateOnly(2026, 7, 24),
            new PartySize(3, 2),
            new Table(7, 4)));
    }

    [Fact]
    public void ConfirmAfterCancellationThrowsInvalidOperationException()
    {
        var reservation = new Reservation(
            new GuestProfile("Ada", "ada@example.com"),
            new DateOnly(2026, 7, 24),
            new PartySize(2, 0),
            new Table(4, 4));

        reservation.Cancel();

        Assert.Throws<InvalidOperationException>(() => reservation.Confirm());
    }

    [Fact]
    public void ReservationBookFindsGuestsAndCountsConfirmedReservations()
    {
        var book = new ReservationBook();
        var first = new Reservation(
            new GuestProfile("Ada", "ada@example.com"),
            new DateOnly(2026, 7, 24),
            new PartySize(2, 0),
            new Table(1, 2));
        first.Confirm();

        var second = new Reservation(
            new GuestProfile("Grace", "grace@example.com"),
            new DateOnly(2026, 7, 24),
            new PartySize(1, 1),
            new Table(2, 4));

        book.Add(first);
        book.Add(second);

        var matches = book.FindByGuest("ADA@EXAMPLE.COM");

        Assert.Single(matches);
        Assert.Same(first, matches[0]);
        Assert.Equal(1, book.CountConfirmedOn(new DateOnly(2026, 7, 24)));
    }

    [Fact]
    public void ReservationBookExposesReadOnlyReservations()
    {
        var book = new ReservationBook();
        book.Add(new Reservation(
            new GuestProfile("Ada", "ada@example.com"),
            new DateOnly(2026, 7, 24),
            new PartySize(1, 0),
            new Table(1, 2)));

        Assert.IsAssignableFrom<IReadOnlyList<Reservation>>(book.Reservations);
        Assert.Single(book.Reservations);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(0, 0)]
    public void PartySizeRejectsInvalidCounts(int adults, int children)
    {
        Assert.ThrowsAny<ArgumentException>(() => new PartySize(adults, children));
    }

    [Fact]
    public void PartySizeRejectsOverflow()
    {
        Assert.Throws<OverflowException>(() => new PartySize(int.MaxValue, 1));
    }

    [Fact]
    public void ReservationRejectsDefaultPartySize()
    {
        Assert.Throws<ArgumentException>(() => new Reservation(
            new GuestProfile("Ada", "ada@example.com"),
            new DateOnly(2026, 7, 24),
            default,
            new Table(1, 2)));
    }

    [Fact]
    public void ReservationBookRejectsNullReservation()
    {
        var book = new ReservationBook();
        Assert.Throws<ArgumentNullException>(() => book.Add(null!));
    }
}
