namespace TestingDependencyBoundariesPractice;

public sealed class OrderService
{
    private readonly IInventoryGateway inventoryGateway;
    private readonly IReceiptStore receiptStore;

    public OrderService(IInventoryGateway inventoryGateway, IReceiptStore receiptStore)
    {
        this.inventoryGateway = inventoryGateway ?? throw new ArgumentNullException(nameof(inventoryGateway));
        this.receiptStore = receiptStore ?? throw new ArgumentNullException(nameof(receiptStore));
    }

    public OrderReceipt PlaceOrder(string orderId, string sku, int quantity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderId, nameof(orderId));
        ArgumentException.ThrowIfNullOrWhiteSpace(sku, nameof(sku));
        ArgumentOutOfRangeException.ThrowIfLessThan(quantity, 1);

        var normalizedOrderId = orderId.Trim();
        var normalizedSku = sku.Trim();
        var available = inventoryGateway.GetAvailable(normalizedSku);
        if (available < quantity)
        {
            throw new InvalidOperationException("Not enough inventory is available.");
        }

        inventoryGateway.Reserve(normalizedSku, quantity);
        var receipt = new OrderReceipt(normalizedOrderId, normalizedSku, quantity);
        receiptStore.Save(receipt);
        return receipt;
    }
}
