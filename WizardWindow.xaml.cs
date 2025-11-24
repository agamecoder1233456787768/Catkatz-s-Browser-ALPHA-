using System.Windows;

namespace NonChromeBrowser;

public partial class WizardWindow : Window
{
    public Config ResultConfig { get; private set; } = new Config();

    public WizardWindow(Config existing)
    {
        InitializeComponent();
        JsCheckbox.IsChecked = existing.JavaScriptEnabled;
        ImagesCheckbox.IsChecked = !existing.BlockImages;
        CookiesCheckbox.IsChecked = existing.ClearCookiesOnExit;
        TrackersCheckbox.IsChecked = existing.BlockTrackers;
        AdsCheckbox.IsChecked = existing.BlockAds;
        HomeUrlBox.Text = existing.HomeUrl;
        foreach (var item in ThemePresetBox.Items)
        {
            if (item is System.Windows.Controls.ComboBoxItem cbi && (string)cbi.Content == existing.ThemePreset)
            {
                ThemePresetBox.SelectedItem = cbi;
                break;
            }
        }
        BackgroundColorBox.Text = existing.ThemeBackground;
        ForegroundColorBox.Text = existing.ThemeForeground;
        AccentColorBox.Text = existing.ThemeAccent;
        foreach (var item in SearchEngineBox.Items)
        {
            if (item is System.Windows.Controls.ComboBoxItem cbi && (string)cbi.Content == existing.SearchEngine)
            {
                SearchEngineBox.SelectedItem = cbi;
                break;
            }
        }
        LiteModeCheckbox.IsChecked = existing.LiteMode;
        var skip = (System.Windows.Controls.CheckBox)FindName("SkipWizardCheckbox");
        if (skip != null) skip.IsChecked = existing.SkipWizard;
    }

    void Save_Click(object sender, RoutedEventArgs e)
    {
        ResultConfig.JavaScriptEnabled = JsCheckbox.IsChecked == true;
        ResultConfig.BlockImages = !(ImagesCheckbox.IsChecked == true);
        ResultConfig.ClearCookiesOnExit = CookiesCheckbox.IsChecked == true;
        ResultConfig.BlockTrackers = TrackersCheckbox.IsChecked == true;
        ResultConfig.BlockAds = AdsCheckbox.IsChecked == true;
        ResultConfig.HomeUrl = string.IsNullOrWhiteSpace(HomeUrlBox.Text) ? "about:blank" : HomeUrlBox.Text;
        if (ThemePresetBox.SelectedItem is System.Windows.Controls.ComboBoxItem cbi)
            ResultConfig.ThemePreset = (string)cbi.Content;
        if (SearchEngineBox.SelectedItem is System.Windows.Controls.ComboBoxItem se)
        {
            var name = (string)se.Content;
            ResultConfig.SearchEngine = name;
            ResultConfig.SearchTemplate = name == "DuckDuckGo" ? "https://duckduckgo.com/?q={query}" : name == "Bing" ? "https://www.bing.com/search?q={query}" : "https://www.google.com/search?q={query}";
        }
        ResultConfig.LiteMode = LiteModeCheckbox.IsChecked == true;
        ResultConfig.ThemeBackground = string.IsNullOrWhiteSpace(BackgroundColorBox.Text) ? "#FFFFFF" : BackgroundColorBox.Text;
        ResultConfig.ThemeForeground = string.IsNullOrWhiteSpace(ForegroundColorBox.Text) ? "#000000" : ForegroundColorBox.Text;
        ResultConfig.ThemeAccent = string.IsNullOrWhiteSpace(AccentColorBox.Text) ? "#3B82F6" : AccentColorBox.Text;
        var skip = (System.Windows.Controls.CheckBox)FindName("SkipWizardCheckbox");
        ResultConfig.SkipWizard = (skip != null && skip.IsChecked == true);
        DialogResult = true;
    }

    void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}