using ConwayLifeWinForms.App.Core.Abstractions;
using ConwayLifeWinForms.App.Core.Domain;
using ConwayLifeWinForms.App.Core.Patterns;
using ConwayLifeWinForms.App.Core.Storage;
using ConwayLifeWinForms.App.Core.Timing;
using ConwayLifeWinForms.App.UI;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace ConwayLifeWinForms.App;
// CON-20: точка входа приложения
internal static class Program
{
    public static ILoggerFactory LoggerFactory { get; private set; } = null!;
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        // Папка для логов рядом с exe
        var logDir = Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(logDir);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                path: Path.Combine(logDir, "app-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                shared: true
            )
            .CreateLogger();

        LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(Log.Logger, dispose: true);
        });

        var log = LoggerFactory.CreateLogger("App");
        log.LogInformation("Game Life starting...");

        System.Windows.Forms.Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

        System.Windows.Forms.Application.ThreadException += (_, e) =>
            log.LogError(e.Exception, "UI thread exception");

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            log.LogCritical(e.ExceptionObject as Exception, "Unhandled exception");


        ILifeEngine engine = new LifeEngine(width: 80, height: 60, new ClassicLifeRules());
        IStateStorage storage = new JsonStateStorage();
        IPatternProvider patternProvider = new DefaultPatternProvider();
        ITickSource tickSource = new FormsTimerTickSource();
        try
        {
            using MainForm form = new(engine, storage, patternProvider, tickSource, LoggerFactory.CreateLogger<MainForm>());
            System.Windows.Forms.Application.Run(form);
        }
        finally
        {
            Log.CloseAndFlush();
        }

    }
}
