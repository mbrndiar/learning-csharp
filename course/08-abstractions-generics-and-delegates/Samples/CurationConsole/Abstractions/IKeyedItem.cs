namespace CurationConsole.Abstractions;

/// <summary>Identifies an item by the stable key used by a curated catalog.</summary>
public interface IKeyedItem
{
    string Key { get; }
}
