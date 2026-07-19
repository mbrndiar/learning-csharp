using TestingDependencyBoundariesPractice;
using Xunit;

namespace TestingDependencyBoundariesPractice.Tests;

public sealed class OrderServiceTests
{
    [Fact]
    public void PlaceOrderReturnsReceiptAndPersistsBoundaryCalls()
    {
        var inventoryGateway = new FakeInventoryGateway();
        inventoryGateway.SetAvailable("COURSE-001", 5);
        var receiptStore = new FakeReceiptStore();
        var service = new OrderService(inventoryGateway, receiptStore);

        var receipt = service.PlaceOrder(" ORDER-42 ", " COURSE-001 ", 2);

        Assert.Equal(new OrderReceipt("ORDER-42", "COURSE-001", 2), receipt);
        Assert.Equal([new ReserveCall("COURSE-001", 2)], inventoryGateway.ReserveCalls);
        Assert.Equal([receipt], receiptStore.SavedReceipts);
    }

    [Fact]
    public void PlaceOrderAllowsExactInventoryMatch()
    {
        var inventoryGateway = new FakeInventoryGateway();
        inventoryGateway.SetAvailable("COURSE-001", 2);
        var service = new OrderService(inventoryGateway, new FakeReceiptStore());

        var receipt = service.PlaceOrder("ORDER-42", "COURSE-001", 2);

        Assert.Equal(2, receipt.Quantity);
        Assert.Single(inventoryGateway.ReserveCalls);
    }

    [Fact]
    public void PlaceOrderRejectsQuantityLessThanOne()
    {
        var service = new OrderService(new FakeInventoryGateway(), new FakeReceiptStore());

        Assert.Throws<ArgumentOutOfRangeException>(() => service.PlaceOrder("ORDER-42", "COURSE-001", 0));
    }

    [Fact]
    public void PlaceOrderThrowsWhenInventoryIsInsufficient()
    {
        var inventoryGateway = new FakeInventoryGateway();
        inventoryGateway.SetAvailable("COURSE-001", 1);
        var receiptStore = new FakeReceiptStore();
        var service = new OrderService(inventoryGateway, receiptStore);

        Assert.Throws<InvalidOperationException>(() => service.PlaceOrder("ORDER-42", "COURSE-001", 2));
        Assert.Empty(inventoryGateway.ReserveCalls);
        Assert.Empty(receiptStore.SavedReceipts);
    }

    [Theory]
    [InlineData(null, "COURSE-001")]
    [InlineData("", "COURSE-001")]
    [InlineData("   ", "COURSE-001")]
    [InlineData("ORDER-42", null)]
    [InlineData("ORDER-42", "")]
    [InlineData("ORDER-42", "   ")]
    public void PlaceOrderRejectsMissingText(string? orderId, string? sku)
    {
        var service = new OrderService(new FakeInventoryGateway(), new FakeReceiptStore());

        Assert.ThrowsAny<ArgumentException>(() => service.PlaceOrder(orderId!, sku!, 1));
    }

    [Fact]
    public void ConstructorRejectsNullDependencies()
    {
        Assert.Throws<ArgumentNullException>(() => new OrderService(null!, new FakeReceiptStore()));
        Assert.Throws<ArgumentNullException>(() => new OrderService(new FakeInventoryGateway(), null!));
    }

    private sealed class FakeInventoryGateway : IInventoryGateway
    {
        private readonly Dictionary<string, int> availability = new(StringComparer.OrdinalIgnoreCase);

        public List<ReserveCall> ReserveCalls { get; } = [];

        public int GetAvailable(string sku) => availability.TryGetValue(sku, out var available) ? available : 0;

        public void Reserve(string sku, int quantity) => ReserveCalls.Add(new ReserveCall(sku, quantity));

        public void SetAvailable(string sku, int quantity) => availability[sku] = quantity;
    }

    private sealed class FakeReceiptStore : IReceiptStore
    {
        public List<OrderReceipt> SavedReceipts { get; } = [];

        public void Save(OrderReceipt receipt) => SavedReceipts.Add(receipt);
    }

    private sealed record ReserveCall(string Sku, int Quantity);
}
