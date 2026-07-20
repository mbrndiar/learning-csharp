using ComparativeKv.Core;

namespace ComparativeKv.Application;

public sealed class ConfigurationApplication
{
    private readonly IConfigurationStore store;

    public ConfigurationApplication(IConfigurationStore store)
    {
        this.store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public SetResult SetValue(string key, JsonValue value, SetExpectation expectation) =>
        store.SetValue(key, value, expectation);

    public Entry GetValue(string key) => store.GetValue(key);

    public DeleteResult DeleteValue(string key, DeleteExpectation expectation) =>
        store.DeleteValue(key, expectation);

    public ListResult ListEntries() => store.ListEntries();
}
