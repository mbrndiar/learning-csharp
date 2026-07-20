namespace Tasks.Server.Configuration;

/// <summary>Selects which persistence adapter a server process uses.</summary>
public enum StorageBackend
{
    /// <summary>The SQLite database repository.</summary>
    Sqlite,

    /// <summary>The versioned Markdown checklist repository.</summary>
    Markdown,
}
