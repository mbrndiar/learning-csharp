namespace AbstractionsGenericsDelegatesPractice;

/// <summary>Decides whether an item is allowed into the catalog.</summary>
public interface IRule<in T>
{
    bool Accepts(T item);
}
