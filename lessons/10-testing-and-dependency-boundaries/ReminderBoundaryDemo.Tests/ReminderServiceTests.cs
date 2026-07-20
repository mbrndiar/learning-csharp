using ReminderBoundaryDemo;
using Xunit;

namespace ReminderBoundaryDemo.Tests;

public sealed class ReminderServiceTests
{
    [Fact]
    public void TrySendDueSoonSendsInsideTheWindow()
    {
        var sink = new FakeReminderSink();
        var service = new ReminderService(sink);

        var sent = service.TrySendDueSoon("ada@example.com", 2);

        Assert.True(sent);
        Assert.Equal(["ada@example.com|Reminder: due in 2 day(s)."], sink.Messages);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(7)]
    public void TrySendDueSoonSkipsOutsideTheWindow(int daysUntilDue)
    {
        var sink = new FakeReminderSink();
        var service = new ReminderService(sink);

        var sent = service.TrySendDueSoon("ada@example.com", daysUntilDue);

        Assert.False(sent);
        Assert.Empty(sink.Messages);
    }

    [Fact]
    public void TrySendDueSoonRejectsNegativeDays()
    {
        var service = new ReminderService(new FakeReminderSink());

        Assert.Throws<ArgumentOutOfRangeException>(() => service.TrySendDueSoon("ada@example.com", -1));
    }

    [Fact]
    public void ConstructorRejectsNullSink()
    {
        Assert.Throws<ArgumentNullException>(() => new ReminderService(null!));
    }

    private sealed class FakeReminderSink : IReminderSink
    {
        public List<string> Messages { get; } = [];

        public void Send(string recipient, string message) => Messages.Add($"{recipient}|{message}");
    }
}
