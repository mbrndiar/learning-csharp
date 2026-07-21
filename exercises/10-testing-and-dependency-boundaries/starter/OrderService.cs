namespace TestingDependencyBoundariesPractice;

public sealed class OrderService
{
    // TODO: Implement this constructor. Reject a missing inventory gateway or receipt store, then retain them as this instance's injected collaborators.
    public OrderService(IInventoryGateway inventoryGateway, IReceiptStore receiptStore) =>
        throw new NotImplementedException("Store the injected boundaries.");

    // TODO: Implement PlaceOrder. Reject blank order id/sku and a quantity below 1, trim the text inputs, check available inventory through the injected gateway, throw InvalidOperationException without reserving or saving when inventory is insufficient, and otherwise reserve, save, and return the receipt.
    public OrderReceipt PlaceOrder(string orderId, string sku, int quantity) =>
        throw new NotImplementedException("Validate inputs, reserve inventory, save the receipt, and return it.");
}
