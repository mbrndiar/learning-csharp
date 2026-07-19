namespace CurationConsole.Abstractions;

public interface IRule<in T>
{
    bool Accepts(T item);
}
