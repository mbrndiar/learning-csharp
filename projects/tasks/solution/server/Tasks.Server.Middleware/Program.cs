using Tasks.Core;
using Tasks.Server.Configuration;
using Tasks.Server.Middleware;
using Tasks.Server.Persistence;

try
{
    ServerSettings settings = ServerSettings.Parse(args);
    ITaskRepository repository = RepositoryFactory.Create(settings.Backend, settings.DataPath);
    var service = new TaskService(repository);

    WebApplication app = TaskServerHost.Build(service);
    app.Urls.Add(settings.BaseUrl);
    Console.WriteLine($"Low-level Task API listening on {settings.BaseUrl} (backend: {settings.Backend})");
    await app.RunAsync();
    return 0;
}
catch (ServerConfigurationException error)
{
    await Console.Error.WriteLineAsync($"configuration error: {error.Message}");
    return 2;
}
