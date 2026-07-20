namespace TestingDependencyBoundariesPractice;

public interface IReceiptStore
{
    void Save(OrderReceipt receipt);
}
