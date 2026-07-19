namespace AbstractionsGenericsDelegatesPractice;

public interface IRule<in T>
{
    bool Accepts(T item);
}
