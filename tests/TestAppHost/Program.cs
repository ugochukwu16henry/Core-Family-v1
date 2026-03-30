using temp_clean_arch.Shared;

namespace temp_clean_arch.TestAppHost;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);

        builder.AddPostgres(Services.DatabaseServer)
            .AddDatabase(Services.Database);

        builder.Build().Run();
    }
}