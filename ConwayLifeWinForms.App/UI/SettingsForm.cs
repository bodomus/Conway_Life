using Microsoft.Extensions.Logging;

namespace ConwayLifeWinForms.App.UI;

public sealed class SettingsForm : Form
{
    private readonly ILogger<SettingsForm> _logger;

    public SettingsForm(ILogger<SettingsForm> logger)
    {
        _logger = logger;

        Text = "Settings";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        Width = 420;
        Height = 160;

        Label infoLabel = new()
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Text = "Application settings will be configured here."
        };

        Controls.Add(infoLabel);

        _logger.LogInformation("SettingsForm created.");
    }
}
