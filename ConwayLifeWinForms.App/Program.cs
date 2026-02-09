using ConwayLifeWinForms.App.Core.Abstractions;
using ConwayLifeWinForms.App.Core.Domain;
using ConwayLifeWinForms.App.Core.Patterns;
using ConwayLifeWinForms.App.Core.Storage;
using ConwayLifeWinForms.App.Core.Timing;
using ConwayLifeWinForms.App.UI;

namespace ConwayLifeWinForms.App;
// CON-20: точка входа приложения
internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        ILifeEngine engine = new LifeEngine(width: 80, height: 60, new ClassicLifeRules());
        IStateStorage storage = new JsonStateStorage();
        IPatternProvider patternProvider = new DefaultPatternProvider();
        ITickSource tickSource = new FormsTimerTickSource();

        using MainForm form = new(engine, storage, patternProvider, tickSource);
        System.Windows.Forms.Application.Run(form);
    }
}
