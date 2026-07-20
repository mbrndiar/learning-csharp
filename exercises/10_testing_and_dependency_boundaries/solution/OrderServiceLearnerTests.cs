using TestingDependencyBoundariesPractice;
using Xunit;

namespace TestingDependencyBoundariesPractice.Tests;

public sealed class OrderServiceLearnerTests
{
    [Fact]
    public void PlaceOrderReservesInventoryAndSavesTheReceipt()
    {
        var inventoryGateway = new FakeInventoryGateway(5);
        var receiptStore = new FakeReceiptStore();
        var service = new OrderService(inventoryGateway, receiptStore);

        var receipt = service.PlaceOrder(" ORDER-42 ", " COURSE-001 ", 2);

        Assert.Equal(new OrderReceipt("ORDER-42", "COURSE-001", 2), receipt);
        Assert.Equal([new ReserveCall("COURSE-001", 2)], inventoryGateway.ReserveCalls);
        Assert.Equal([receipt], receiptStore.SavedReceipts);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void PlaceOrderRejectsNonPositiveQuantities(int quantity)
    {
        var service = new OrderService(new FakeInventoryGateway(5), new FakeReceiptStore());

        Assert.Throws<ArgumentOutOfRangeException>(() => service.PlaceOrder("ORDER-42", "COURSE-001", quantity));
    }

    private sealed class FakeInventoryGateway(int available) : IInventoryGateway
    {
        public List<ReserveCall> ReserveCalls { get; } = [];

        public int GetAvailable(string sku) => available;

        public void Reserve(string sku, int quantity) => ReserveCalls.Add(new ReserveCall(sku, quantity));
    }

    private sealed class FakeReceiptStore : IReceiptStore
    {
        public List<OrderReceipt> SavedReceipts { get; } = [];

        public void Save(OrderReceipt receipt) => SavedReceipts.Add(receipt);
    }

    private sealed record ReserveCall(string Sku, int Quantity);
}
