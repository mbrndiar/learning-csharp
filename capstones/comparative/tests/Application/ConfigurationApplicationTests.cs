using System.Collections.Immutable;
using ComparativeKv.Application;
using ComparativeKv.Core;

namespace ComparativeKv.Tests.Application;

public sealed class ConfigurationApplicationTests
{
    [Fact]
    public void ConstructorRejectsANullStore()
    {
        Assert.Throws<ArgumentNullException>(() => new ConfigurationApplication(null!));
    }

    [Fact]
    public void ApplicationDelegatesEveryOperationToItsInjectedStore()
    {
        using var store = new RecordingStore();
        var application = new ConfigurationApplication(store);
        var value = new JsonStringValue("value");

        var set = application.SetValue("key", value, SetExpectation.Absent);
        var get = application.GetValue("key");
        var delete = application.DeleteValue("key", DeleteExpectation.Exact(1));
        var list = application.ListEntries();

        Assert.Equal(1, store.SetCalls);
        Assert.Equal(1, store.GetCalls);
        Assert.Equal(1, store.DeleteCalls);
        Assert.Equal(1, store.ListCalls);
        Assert.Equal("key", set.Entry.Key);
        Assert.Equal("key", get.Key);
        Assert.Equal("key", delete.Key);
        Assert.Empty(list.Entries);
    }

    private sealed class RecordingStore : IConfigurationStore
    {
        public int SetCalls { get; private set; }

        public int GetCalls { get; private set; }

        public int DeleteCalls { get; private set; }

        public int ListCalls { get; private set; }

        public SetResult SetValue(string key, JsonValue value, SetExpectation expectation)
        {
            SetCalls++;
            return new SetResult(new Entry(key, value, 1), true);
        }

        public Entry GetValue(string key)
        {
            GetCalls++;
            return new Entry(key, new JsonNullValue(), 1);
        }

        public DeleteResult DeleteValue(string key, DeleteExpectation expectation)
        {
            DeleteCalls++;
            return new DeleteResult(key, 1, 2);
        }

        public ListResult ListEntries()
        {
            ListCalls++;
            return new ListResult(ImmutableArray<Entry>.Empty, 2);
        }

        public void Dispose()
        {
        }
    }
}
