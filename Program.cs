using H3CSwitchPortMonitor;
using H3CSwitchPortMonitor.Models;
using H3CSwitchPortMonitor.Services;
using Serilog;

try
{
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .WriteTo.File(
            "logs/app.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7,
            fileSizeLimitBytes: 10 * 1024 * 1024,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
        .CreateLogger();

    Log.Information("H3C Switch Port Monitor starting...");

    Host.CreateDefaultBuilder(args)
        .UseWindowsService(options =>
        {
            options.ServiceName = "H3CSwitchPortMonitor";
        })
        .UseSerilog()
        .ConfigureServices((context, services) =>
        {
            services.Configure<MonitorOptions>(context.Configuration.GetSection("Monitor"));
            services.AddHttpClient<FeishuNotifier>();
            services.AddSingleton<ISnmpClient, SharpSnmpClient>();
            services.AddSingleton<WindowsFirewallConfigurator>();
            services.AddSingleton<PortStateStore>();
            services.AddHostedService<Worker>();
        })
        .Build()
        .Run();
}
catch (Exception ex) when (ex is not OperationCanceledException)
{
    Log.Fatal(ex, "Program failed to start");
    Environment.ExitCode = 1;
}
finally
{
    Log.CloseAndFlush();
}
