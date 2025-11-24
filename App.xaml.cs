using System.Configuration;
using System.Data;
using System.Windows;

namespace NonChromeBrowser;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        AppState.Incognito = e.Args != null && System.Array.Exists(e.Args, a => string.Equals(a, "--incognito", System.StringComparison.OrdinalIgnoreCase));
        if (AppState.Incognito)
        {
            CookieManager.EnableNoPersist();
            CookieManager.ClearAll();
        }
        CatkatzEngine.Apply();
        var cfg = Config.Load();
        var firstRun = !System.IO.File.Exists(Config.ConfigPath);
        if (firstRun && !cfg.SkipWizard && !AppState.Incognito)
        {
            var wiz = new WizardWindow(cfg) { Owner = null };
            var ok = wiz.ShowDialog();
            if (ok == true)
            {
                cfg = wiz.ResultConfig;
                cfg.Save();
            }
        }
        var main = new MainWindow(cfg, AppState.Incognito);
        main.Show();
    }
}

