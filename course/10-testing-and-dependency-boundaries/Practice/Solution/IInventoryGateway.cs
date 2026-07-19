namespace TestingDependencyBoundariesPractice;

public interface IInventoryGateway
{
    int GetAvailable(string sku);

    void Reserve(string sku, int quantity);
}
