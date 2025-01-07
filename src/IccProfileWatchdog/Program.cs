using IccProfileWatchdog.Cache;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace IccProfileWatchdog;

public static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        using var _mutex = new Mutex(true, "Global\\000E4E26-D7DB-4201-8044-B7C1FCAE3E78", out var isFirstInstance);
        if (!isFirstInstance)
        {
            return;
        }

        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var loggerConfiguration = new LoggerConfiguration()
            .WriteTo.File(
                Path.Combine(Application.StartupPath, "IccProfileWatchdog.txt"),
                rollingInterval: RollingInterval.Day);

#if DEBUG
        loggerConfiguration.MinimumLevel.Debug();
#else
        if (args.Length > 0 && args[0] == "--debug")
        {
            loggerConfiguration.MinimumLevel.Debug();
        }
#endif

        var builder = new HostBuilder()
           .ConfigureServices((hostContext, services) =>
           {
               services.AddSingleton<ProfileWatchdog>();
               services.AddSingleton<GammaRampCache>();
               services.AddSingleton<MyApplicationContext>();

           })
           .UseSerilog(loggerConfiguration.CreateLogger());

        var host = builder.Build();

        using var serviceScope = host.Services.CreateScope();
        var services = serviceScope.ServiceProvider;
        var applicationContext = services.GetRequiredService<MyApplicationContext>();
        Application.Run(applicationContext);
    }
}
