using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace ConwayLifeWinForms.App.Bootstrapper
{
    internal static class LoggingBootstrapper
    {
        public static void Init(string appName, Action<LoggerConfiguration>? configure = null)
        {
            var logDir = Path.Combine(AppContext.BaseDirectory, "logs");
            Directory.CreateDirectory(logDir);

            var cfg = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("app", appName)
                .WriteTo.Console()
                .WriteTo.File(
                    path: Path.Combine(logDir, $"{appName}-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 14,
                    shared: true
                );

            configure?.Invoke(cfg);

            Log.Logger = cfg.CreateLogger();
        }

        public static void HookGlobalHandlers(ILogger logger)
        {
            // ����� WinForms ����������� ���������� UI-������ � ThreadException
            System.Windows.Forms.Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            System.Windows.Forms.Application.ThreadException += (_, e) =>
                logger.LogError(e.Exception, "UI thread exception");

            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
                logger.LogCritical(e.ExceptionObject as Exception, "Unhandled exception");
        }

        public static void Shutdown()
        {
            Log.CloseAndFlush();
        }
    }
}
