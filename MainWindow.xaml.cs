using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NonChromeBrowser;

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;

public partial class MainWindow : Window
{
    readonly Config _config;
    readonly bool _incognito;
    readonly string[] _blocked = new[]
    {
        "doubleclick.net",
        "google-analytics.com",
        "googletagmanager.com",
        "googlesyndication.com",
        "adservice.google.com",
        "adsafeprotected.com",
        "adroll.com",
        "adnxs.com",
        "rubiconproject.com",
        "imrworldwide.com",
        "scorecardresearch.com",
        "quantserve.com",
        "outbrain.com",
        "facebook.net",
        "connect.facebook.net",
        "bing.com",
        "ads.yahoo.com",
        "taboola.com",
        "criteo.com"
    };

    readonly string[] _modernSites = new[]
    {
        "youtube.com","youtu.be","google.com","gemini.google.com","accounts.google.com","docs.google.com"
    };

    public MainWindow(Config config, bool incognito = false)
    {
        InitializeComponent();
        _config = config;
        _incognito = incognito;
        AddressBar.Text = _config.LocalHomePath();
        StatusText.Text = "";
        Closing += (_, __) => { if (_config.ClearCookiesOnExit || _incognito) EndBrowserSession(); };
        if (string.IsNullOrWhiteSpace(_config.HomeUrl)) _config.HomeUrl = "about:blank";
        CreateTab(_config.LocalHomePath(), true);
        if (_incognito)
        {
            Title += " — Incognito";
            EndBrowserSession();
        }
        ApplyTheme();
    }

    void Navigate(string url, WebBrowser? wb = null)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        var s = url.Trim();
        bool looksLikeUrl = s.Contains('.') || s.StartsWith("http", StringComparison.OrdinalIgnoreCase) || s.StartsWith("about:");
        if (!looksLikeUrl)
        {
            var q = Uri.EscapeDataString(s);
            url = (_config.SearchTemplate ?? "https://www.google.com/search?q={query}").Replace("{query}", q);
        }
        else if (!s.StartsWith("http", StringComparison.OrdinalIgnoreCase) && !s.StartsWith("about:"))
        {
            url = "https://" + s;
        }
        AddressBar.Text = url;
        var target = wb ?? CurrentBrowser();
        target?.Navigate(url);
    }

    void BackButton_Click(object sender, RoutedEventArgs e)
    {
        var b = CurrentBrowser();
        if (b != null && b.CanGoBack) b.GoBack();
    }

    void ForwardButton_Click(object sender, RoutedEventArgs e)
    {
        var f = CurrentBrowser();
        if (f != null && f.CanGoForward) f.GoForward();
    }

    void ReloadButton_Click(object sender, RoutedEventArgs e)
    {
        CurrentBrowser()?.Refresh();
    }

    void HomeButton_Click(object sender, RoutedEventArgs e)
    {
        Navigate(_config.LocalHomePath());
    }

    void GoButton_Click(object sender, RoutedEventArgs e)
    {
        Navigate(AddressBar.Text);
    }

    void AddressBar_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) Navigate(AddressBar.Text);
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            if (e.Key == Key.T) CreateTab(_config.HomeUrl, true);
            else if (e.Key == Key.W)
            {
                if (Tabs.SelectedItem is TabItem ti) Tabs.Items.Remove(ti);
            }
            else if (e.Key == Key.L) AddressBar.Focus();
        }
    }

    void Browser_Navigating(object sender, NavigatingCancelEventArgs e)
    {
        StatusText.Text = e.Uri?.ToString() ?? "";
        Progress.Value = 10;
        Progress.Visibility = Visibility.Visible;
        var wb = sender as WebBrowser;
        if (wb != null) SetSilent(wb, true);
        if (_config.BlockTrackers && e.Uri != null)
        {
            var host = e.Uri.Host.ToLowerInvariant();
            if (_blocked.Any(d => host == d || host.EndsWith("." + d)))
            {
                e.Cancel = true;
                StatusText.Text = "Blocked: " + host;
                return;
            }
        }
        wb = sender as WebBrowser;
        if (wb != null && e.Uri != null)
        {
            var h = e.Uri.Host.ToLowerInvariant();
            wb.Tag = _modernSites.Any(d => h == d || h.EndsWith("." + d)) ? "modern" : null;
        }
    }

    void Browser_LoadCompleted(object sender, NavigationEventArgs e)
    {
        try
        {
            var wb = sender as WebBrowser;
            if (wb != null) SetSilent(wb, true);
            var modern = (wb?.Tag as string) == "modern";
            Progress.Value = 100; Progress.Visibility = Visibility.Collapsed;
            if (_config.LiteMode)
            {
                Inject(wb, "var s=document.createElement('style');s.innerHTML='video,iframe{display:none!important}';document.head.appendChild(s);");
            }
            if (!modern && _config.BlockImages) Inject(wb, "var s=document.createElement('style');s.innerHTML='img,video,canvas{display:none!important}';document.head.appendChild(s);");
            if (!modern && !_config.JavaScriptEnabled)
            {
                Inject(wb, "document.querySelectorAll('script').forEach(function(x){x.remove()});");
                Inject(wb, "window.eval=function(){};window.Function=function(){};document.createElement=function(n){if(n==='script')return document.createElement('noscript');return HTMLElement.prototype.createElement?n=HTMLElement.prototype.createElement.call(document,n):document.createElement(n)};");
            }
            Inject(wb, @"(function(){
                if(!Element.prototype.closest){Element.prototype.closest=function(s){var el=this;do{if(el.matches && el.matches(s))return el;el=el.parentElement||el.parentNode;}while(el!==null && el.nodeType===1);return null;};}
                if(!String.prototype.startsWith){String.prototype.startsWith=function(s,pos){pos=pos||0;return this.substring(pos,pos+s.length)===s;};}
                if(!String.prototype.endsWith){String.prototype.endsWith=function(s){return this.substring(this.length-s.length)===s;};}
                if(!Object.assign){Object.assign=function(t){t=t||{};for(var i=1;i<arguments.length;i++){var s=arguments[i];for(var k in s){if(Object.prototype.hasOwnProperty.call(s,k)) t[k]=s[k];}}return t;};}
                if(window.NodeList && !NodeList.prototype.forEach){NodeList.prototype.forEach=Array.prototype.forEach;}
            })();");
            if (modern)
            {
                Inject(wb, @"(function(){
                    if(typeof Promise==='undefined'){
                        window.Promise=function(executor){var c=[];var r=false;var v;function res(x){if(r)return;r=true;v=x;c.forEach(function(f){try{f(x);}catch(e){}});}try{executor(res,function(){})}catch(e){};this.then=function(f){if(r){try{f(v);}catch(e){}}else c.push(f);return this;};};
                    }
                    if(typeof window.fetch==='undefined'){
                        window.fetch=function(url,opts){return new Promise(function(resolve,reject){try{var x=new XMLHttpRequest();x.open((opts&&opts.method)||'GET',url,true);if(opts&&opts.headers){for(var k in opts.headers){x.setRequestHeader(k,opts.headers[k]);}}x.onload=function(){resolve({ok:(x.status>=200&&x.status<300),status:x.status,text:function(){return Promise.resolve(x.responseText);},json:function(){try{return Promise.resolve(JSON.parse(x.responseText));}catch(e){return Promise.resolve({});}}});};x.onerror=function(){reject(new Error('Network error'));};if(opts&&opts.body)x.send(opts.body);else x.send();}catch(e){reject(e);}});};
                    }
                })();");
            }
            if (_config.BlockAds)
            {
                var script = @"(function(){var d=document;var rm=function(sel){try{d.querySelectorAll(sel).forEach(function(x){x.remove()})}catch(e){}};rm('script[src*=""doubleclick""],script[src*=""googlesyndication""],script[src*=""adservice""],script[src*=""adsafeprotected""],script[src*=""adroll""],script[src*=""adnxs""],script[src*=""rubicon""],script[src*=""scorecardresearch""],script[src*=""quantserve""],script[src*=""taboola""],script[src*=""outbrain""],script[src*=""criteo""],iframe[src*=""doubleclick""],iframe[src*=""googlesyndication""],iframe[src*=""adservice""],iframe[src*=""adnxs""],iframe[src*=""rubicon""],iframe[src*=""taboola""],iframe[src*=""outbrain""],iframe[src*=""criteo""]');var s=document.createElement('style');s.innerHTML='iframe[src*=""ads""],iframe[src*=""doubleclick""],iframe[src*=""googlesyndication""],div[id*=""ad""],div[class*=""ad""],div[id*=""banner""],div[class*=""banner""],div[class*=""sponsor""],div[id*=""sponsor""],.ad,.ads,.advert,.sponsored,.promotion{display:none!important}';d.head.appendChild(s);window.open=function(){return null};})();";
                Inject(wb, script);
            }
            Inject(wb, @"(function(){
                try{
                    window.onerror=function(){return true};
                    if(typeof window.onunhandledrejection==='undefined'){
                        window.onunhandledrejection=function(){return true};
                    }
                    if(window.console){var noop=function(){};console.error=noop;console.warn=noop;}
                }catch(e){}
            })();");
            if (_incognito)
            {
                var incog = @"(function(){
                    try{localStorage && localStorage.clear();}catch(e){}
                    try{sessionStorage && sessionStorage.clear();}catch(e){}
                    try{Object.defineProperty(window,'localStorage',{get:function(){return {getItem:function(){return null},setItem:function(){},removeItem:function(){},clear:function(){}}}});}catch(e){}
                    try{Object.defineProperty(window,'sessionStorage',{get:function(){return {getItem:function(){return null},setItem:function(){},removeItem:function(){},clear:function(){}}}});}catch(e){}
                    try{if(window.indexedDB){var open=window.indexedDB.open;window.indexedDB.open=function(){return {onsuccess:null,onerror:null}};}}catch(e){}
                })();";
                Inject(wb, incog);
            }
            UpdateTabHeaderTitle(wb);
        }
        catch { }
    }

    void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var wiz = new WizardWindow(_config) { Owner = this };
        var ok = wiz.ShowDialog();
        if (ok == true)
        {
            var oldHome = _config.HomeUrl;
            var changedHome = oldHome != wiz.ResultConfig.HomeUrl;
            _config.JavaScriptEnabled = wiz.ResultConfig.JavaScriptEnabled;
            _config.BlockImages = wiz.ResultConfig.BlockImages;
            _config.ClearCookiesOnExit = wiz.ResultConfig.ClearCookiesOnExit;
            _config.BlockTrackers = wiz.ResultConfig.BlockTrackers;
            _config.BlockAds = wiz.ResultConfig.BlockAds;
            _config.HomeUrl = wiz.ResultConfig.HomeUrl;
            _config.ThemeBackground = wiz.ResultConfig.ThemeBackground;
            _config.ThemeForeground = wiz.ResultConfig.ThemeForeground;
            _config.ThemeAccent = wiz.ResultConfig.ThemeAccent;
            if (!_incognito) _config.Save();
            ApplyTheme();
            if (changedHome) Navigate(_config.HomeUrl);
            else ReloadButton_Click(sender, e);
        }
    }

    void Inject(WebBrowser? wb, string script)
    {
        if (wb == null) wb = CurrentBrowser();
        dynamic doc = wb?.Document;
        dynamic win = doc != null ? doc.parentWindow : null;
        if (win != null) win.execScript(script, "JavaScript");
    }

    static void SetSilent(WebBrowser browser, bool silent)
    {
        var field = typeof(WebBrowser).GetProperty("ActiveXInstance", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field != null)
        {
            var activeX = field.GetValue(browser, null);
            if (activeX != null)
            {
                var prop = activeX.GetType().GetProperty("Silent");
                prop?.SetValue(activeX, silent, null);
            }
        }
    }

    [DllImport("wininet.dll", SetLastError = true)]
    static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);
    static void EndBrowserSession()
    {
        InternetSetOption(IntPtr.Zero, 42, IntPtr.Zero, 0);
    }

    WebBrowser? CurrentBrowser()
    {
        if (Tabs.SelectedContent is Grid g)
            return g.Children.OfType<WebBrowser>().FirstOrDefault();
        return null;
    }

    void CreateTab(string url, bool select)
    {
        var wb = new WebBrowser();
        SetSilent(wb, true);
        wb.Navigating += Browser_Navigating;
        wb.Navigated += Browser_Navigated;
        wb.LoadCompleted += Browser_LoadCompleted;
        var g = new Grid();
        g.Background = ParseBrush(_config.ThemeBackground) ?? Brushes.White;
        g.Children.Add(wb);
        var title = new TextBlock { Text = "New Tab", VerticalAlignment = VerticalAlignment.Center };
        var close = new Button { Content = "✖", Width = 24, Height = 24, Margin = new Thickness(6,0,0,0), Padding = new Thickness(0)};
        var header = new StackPanel { Orientation = Orientation.Horizontal };
        header.Children.Add(title);
        header.Children.Add(close);
        var tab = new TabItem { Header = header, Content = g };
        close.Click += (s, e) => { Tabs.Items.Remove(tab); };
        Tabs.Items.Add(tab);
        if (select) Tabs.SelectedItem = tab;
        Navigate(url, wb);
    }

    void Browser_Navigated(object sender, NavigationEventArgs e)
    {
        var wb = sender as WebBrowser;
        if (wb != null) SetSilent(wb, true);
    }

    Brush? ParseBrush(string hex)
    {
        try
        {
            var bc = new BrushConverter();
            var br = bc.ConvertFromString(hex) as Brush;
            return br;
        }
        catch { return null; }
    }

    void ApplyTheme()
    {
        try
        {
            Brush bg, fg, ac;
            switch ((_config.ThemePreset ?? "Light").ToLowerInvariant())
            {
                case "dark":
                    bg = ParseBrush("#1E1E1E") ?? Brushes.Black;
                    fg = ParseBrush("#D1D5DB") ?? Brushes.White;
                    ac = ParseBrush("#2563EB") ?? Brushes.SlateBlue;
                    break;
                case "black":
                    bg = ParseBrush("#000000") ?? Brushes.Black;
                    fg = ParseBrush("#FFFFFF") ?? Brushes.White;
                    ac = ParseBrush("#444444") ?? Brushes.Gray;
                    break;
                case "green":
                    bg = ParseBrush("#064E3B") ?? Brushes.DarkGreen;
                    fg = ParseBrush("#ECFDF5") ?? Brushes.White;
                    ac = ParseBrush("#10B981") ?? Brushes.SeaGreen;
                    break;
                case "blue":
                    bg = ParseBrush("#0F172A") ?? Brushes.Navy;
                    fg = ParseBrush("#E5E7EB") ?? Brushes.White;
                    ac = ParseBrush("#3B82F6") ?? Brushes.DodgerBlue;
                    break;
                case "custom":
                    bg = ParseBrush(_config.ThemeBackground) ?? Brushes.White;
                    fg = ParseBrush(_config.ThemeForeground) ?? Brushes.Black;
                    ac = ParseBrush(_config.ThemeAccent) ?? Brushes.SlateBlue;
                    break;
                default:
                    bg = ParseBrush("#FFFFFF") ?? Brushes.White;
                    fg = ParseBrush("#000000") ?? Brushes.Black;
                    ac = ParseBrush("#3B82F6") ?? Brushes.SlateBlue;
                    break;
            }

            Background = bg;
            if (TopBar != null)
            {
                TopBar.Background = bg;
                foreach (var b in TopBar.Children.OfType<StackPanel>().SelectMany(sp => sp.Children.OfType<Button>()))
                {
                    b.Background = ac;
                    b.Foreground = fg;
                }
                GoButton.Background = ac; GoButton.Foreground = fg;
                SettingsButton.Background = ac; SettingsButton.Foreground = fg;
            }
            if (BottomStatus != null)
            {
                BottomStatus.Background = bg;
                StatusText.Foreground = fg;
            }
            AddressBar.Background = bg; AddressBar.Foreground = fg;
        }
        catch { }
    }

    void UpdateTabHeaderTitle(WebBrowser? wb)
    {
        if (wb == null) return;
        foreach (TabItem ti in Tabs.Items)
        {
            if (ti.Content is Grid g && g.Children.OfType<WebBrowser>().FirstOrDefault() == wb)
            {
                var header = ti.Header as StackPanel;
                var t = header?.Children.OfType<TextBlock>().FirstOrDefault();
                if (t != null)
                {
                    try
                    {
                        dynamic d = wb.Document;
                        t.Text = d?.Title ?? "Tab";
                    }
                    catch { }
                }
                break;
            }
        }
    }

    void NewTabButton_Click(object sender, RoutedEventArgs e) => CreateTab(_config.HomeUrl, true);
    void CloseTabButton_Click(object sender, RoutedEventArgs e)
    {
        if (Tabs.SelectedItem is TabItem ti) Tabs.Items.Remove(ti);
    }

    void IncognitoButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var exe = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(exe))
            {
                var psi = new System.Diagnostics.ProcessStartInfo(exe, "--incognito") { UseShellExecute = true };
                System.Diagnostics.Process.Start(psi);
            }
        }
        catch { }
    }

    void OpenExternalButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var url = AddressBar.Text;
            if (!string.IsNullOrWhiteSpace(url))
            {
                if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) url = "https://" + url;
                var psi = new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true };
                System.Diagnostics.Process.Start(psi);
            }
        }
        catch { }
    }
}