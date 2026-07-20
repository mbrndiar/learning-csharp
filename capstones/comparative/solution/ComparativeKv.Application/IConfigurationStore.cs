using ComparativeKv.Core;

namespace ComparativeKv.Application;

public interface IConfigurationStore : IDisposable
{
    SetResult SetValue(string key, JsonValue value, SetExpectation expectation);

    Entry GetValue(string key);

    DeleteResult DeleteValue(string key, DeleteExpectation expectation);

    ListResult ListEntries();
}
