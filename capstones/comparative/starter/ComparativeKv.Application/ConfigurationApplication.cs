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
        UseStoreAndThrow<SetResult>("comparative application set");

    public Entry GetValue(string key) => UseStoreAndThrow<Entry>("comparative application get");

    public DeleteResult DeleteValue(string key, DeleteExpectation expectation) =>
        UseStoreAndThrow<DeleteResult>("comparative application delete");

    public ListResult ListEntries() => UseStoreAndThrow<ListResult>("comparative application list");

    private T UseStoreAndThrow<T>(string feature)
    {
        _ = store;
        return MilestoneIncomplete.Throw<T>(feature);
    }
}
