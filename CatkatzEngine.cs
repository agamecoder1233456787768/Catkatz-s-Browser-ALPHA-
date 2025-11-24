using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace NonChromeBrowser;

static class CatkatzEngine
{
    const int IE11_EMULATION = 11000;

    [DllImport("urlmon.dll", CharSet = CharSet.Ansi)]
    static extern int UrlMkSetSessionOption(int dwOption, string pBuffer, int dwBufferLength, int dwReserved);

    const int URLMON_OPTION_USERAGENT = 0x10000001;

    public static void Apply()
    {
        try { SetEmulationForProcess(); } catch { }
        try { SetPerformanceFeatures(); } catch { }
        try { SetUserAgent(BuildUserAgent()); } catch { }
    }

    static string BuildUserAgent()
    {
        var os = "Windows NT 10.0; Win64; x64";
        var ver = "1.0";
        return $"CatkatzBrowser/{ver} ({os}) AppleWebKit/537.36 (KHTML, like Gecko) CatkatzEngine/{ver} Safari/537.36";
    }

    static void SetEmulationForProcess()
    {
        var exeName = Process.GetCurrentProcess().ProcessName + ".exe";
        using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION");
        key?.SetValue(exeName, IE11_EMULATION, RegistryValueKind.DWord);
    }

    static void SetPerformanceFeatures()
    {
        var exeName = Process.GetCurrentProcess().ProcessName + ".exe";
        void Set(string feature, uint value)
        {
            using var k = Registry.CurrentUser.CreateSubKey($@"Software\Microsoft\Internet Explorer\Main\FeatureControl\{feature}");
            k?.SetValue(exeName, value, RegistryValueKind.DWord);
        }
        Set("FEATURE_GPU_RENDERING", 1);
        Set("FEATURE_DOMSTORAGE", 1);
        Set("FEATURE_WEBSOCKET", 1);
        Set("FEATURE_AJAX_CONNECTIONEVENTS", 1);
        Set("FEATURE_DISABLE_NAVIGATION_SOUNDS", 1);
        Set("FEATURE_WEBOC_DOCUMENT_ZOOM", 1);
        Set("FEATURE_96DPI_PIXEL", 1);
        Set("FEATURE_NINPUT_LEGACYMODE", 0);
        Set("FEATURE_WINDOW_RESTRICTIONS", 0);
        Set("FEATURE_WEBOC_POPUPMANAGEMENT", 0);
        Set("FEATURE_ADDON_MANAGEMENT", 0);
        Set("FEATURE_DISABLE_SCRIPT_DEBUGGING", 1);
        Set("FEATURE_DISABLE_SCRIPT_DEBUGGING_IE", 1);
    }

    static void SetUserAgent(string ua)
    {
        UrlMkSetSessionOption(URLMON_OPTION_USERAGENT, ua, ua.Length, 0);
    }
}