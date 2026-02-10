using ConwayLifeWinForms.App.Bootstrapper;
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
        LoggerFactory = LoggingBootstrapper.Init("GameLife");
        var log = LoggerFactory.CreateLogger("App");

        LoggingBootstrapper.HookGlobalHandlers(log);
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
