using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ReadingLog.Tests.TestInfrastructure;

internal sealed class ReadingLogApiFactory : WebApplicationFactory<Program>
{
    private readonly TestDirectory _storageDirectory = new("api-tests");

    public string StorageDirectory => _storageDirectory.Path;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ReadingLog:StorageDirectory", _storageDirectory.Path);
        builder.UseSetting("ReadingLog:StorageFileName", "reading-log.json");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _storageDirectory.Dispose();
        }
    }
}
