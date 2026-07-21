namespace TestingDependencyBoundariesPractice;

public sealed class OrderService
{
    // TODO: Reject missing boundaries and retain the injected collaborators instead of taking ownership of external work.
    public OrderService(IInventoryGateway inventoryGateway, IReceiptStore receiptStore) =>
        throw new NotImplementedException("Store the injected boundaries.");

    // TODO: Validate and normalize inputs, avoid side effects when inventory is insufficient, then coordinate reservation and receipt persistence.
    public OrderReceipt PlaceOrder(string orderId, string sku, int quantity) =>
        throw new NotImplementedException("Validate inputs, reserve inventory, save the receipt, and return it.");
}
