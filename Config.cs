using System;
using System.IO;
using System.Text.Json;

namespace NonChromeBrowser;

public class Config
{
    public bool JavaScriptEnabled { get; set; } = false;
    public bool BlockImages { get; set; } = true;
    public bool ClearCookiesOnExit { get; set; } = true;
    public bool BlockTrackers { get; set; } = true;
    public bool BlockAds { get; set; } = true;
    public string HomeUrl { get; set; } = "about:blank";
    public bool SkipWizard { get; set; } = false;
    public string ThemePreset { get; set; } = "Light";
    public string ThemeBackground { get; set; } = "#FFFFFF";
    public string ThemeForeground { get; set; } = "#000000";
    public string ThemeAccent { get; set; } = "#3B82F6";
    public string SearchEngine { get; set; } = "Google";
    public string SearchTemplate { get; set; } = "https://www.google.com/search?q={query}";
    public bool LiteMode { get; set; } = false;
    public string LocalHomePath()
    {
        var p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "home.html");
        return File.Exists(p) ? new Uri(p).AbsoluteUri : HomeUrl;
    }

    public static string ConfigPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

    public static Config Load()
    {
        if (!File.Exists(ConfigPath)) return new Config();
        var json = File.ReadAllText(ConfigPath);
        var cfg = JsonSerializer.Deserialize<Config>(json);
        return cfg ?? new Config();
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, json);
    }
}