using ConwayLifeWinForms.App.Bootstrapper;
using ConwayLifeWinForms.App.Infrastructure;
using ConwayLifeWinForms.App.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ConwayLifeWinForms.App;

// CON-20: точка входа приложения
internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        LoggingBootstrapper.Init("GameLife");

        using ServiceProvider serviceProvider = CompositionRoot.BuildServiceProvider();
        ILoggerFactory loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        ILogger log = loggerFactory.CreateLogger("App");

        LoggingBootstrapper.HookGlobalHandlers(log);
        log.LogInformation("Game Life starting...");

        try
        {
            Application.Run(serviceProvider.GetRequiredService<MainForm>());
        }
        finally
        {
            LoggingBootstrapper.Shutdown();
        }
    }
}
