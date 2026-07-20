using ComparativeKv.Application;
using ComparativeKv.Core;

namespace ComparativeKv.Storage.Sqlite;

public sealed class SqliteConfigurationStore : IConfigurationStore
{
    private SqliteConfigurationStore()
    {
    }

    public static SqliteConfigurationStore Open(string path) =>
        MilestoneIncomplete.Throw<SqliteConfigurationStore>("comparative SQLite storage");

    public SetResult SetValue(string key, JsonValue value, SetExpectation expectation) =>
        MilestoneIncomplete.Throw<SetResult>("comparative SQLite set");

    public Entry GetValue(string key) => MilestoneIncomplete.Throw<Entry>("comparative SQLite get");

    public DeleteResult DeleteValue(string key, DeleteExpectation expectation) =>
        MilestoneIncomplete.Throw<DeleteResult>("comparative SQLite delete");

    public ListResult ListEntries() => MilestoneIncomplete.Throw<ListResult>("comparative SQLite list");

    public void Dispose()
    {
    }
}
