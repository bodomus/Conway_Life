using ConwayLifeWinForms.App.Core.Abstractions;
using ConwayLifeWinForms.App.Core.Domain;
using ConwayLifeWinForms.App.Core.Patterns;
using ConwayLifeWinForms.App.Core.Storage;
using ConwayLifeWinForms.App.Core.Timing;
using ConwayLifeWinForms.App.UI;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace ConwayLifeWinForms.App.Infrastructure;

internal static class CompositionRoot
{
    public static ServiceProvider BuildServiceProvider()
    {
        ServiceCollection services = new();

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(Log.Logger, dispose: false);
        });

        services.AddSingleton<ILifeRules, ClassicLifeRules>();
        services.AddSingleton<ILifeEngine>(sp =>
            new LifeEngine(width: 80, height: 60, sp.GetRequiredService<ILifeRules>()));
        services.AddSingleton<IStateStorage, JsonStateStorage>();
        services.AddSingleton<IPatternProvider, DefaultPatternProvider>();
        services.AddSingleton<ITickSource, FormsTimerTickSource>();

        services.AddTransient<MainForm>();
        services.AddTransient<SettingsForm>();

        return services.BuildServiceProvider();
    }
}
