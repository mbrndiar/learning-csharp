namespace TestingDependencyBoundariesPractice;

public sealed class OrderService
{
    public OrderService(IInventoryGateway inventoryGateway, IReceiptStore receiptStore) =>
        throw new NotImplementedException("Store the injected boundaries.");

    public OrderReceipt PlaceOrder(string orderId, string sku, int quantity) =>
        throw new NotImplementedException("Validate inputs, reserve inventory, save the receipt, and return it.");
}
