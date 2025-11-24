using System;
using System.Runtime.InteropServices;

namespace NonChromeBrowser;

static class CookieManager
{
    const int INTERNET_OPTION_SUPPRESS_BEHAVIOR = 81;
    const int INTERNET_SUPPRESS_COOKIE_PERSIST = 3;

    [DllImport("wininet.dll", SetLastError = true)]
    static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);

    [DllImport("wininet.dll", SetLastError = true, CharSet = CharSet.Auto)]
    static extern IntPtr FindFirstUrlCacheEntry(string lpszUrlSearchPattern, IntPtr lpFirstCacheEntryInfo, ref uint lpdwFirstCacheEntryInfoBufferSize);

    [DllImport("wininet.dll", SetLastError = true, CharSet = CharSet.Auto)]
    static extern bool FindNextUrlCacheEntry(IntPtr hEnumHandle, IntPtr lpNextCacheEntryInfo, ref uint lpdwNextCacheEntryInfoBufferSize);

    [DllImport("wininet.dll", SetLastError = true)]
    static extern bool FindCloseUrlCache(IntPtr hEnumHandle);

    [DllImport("wininet.dll", SetLastError = true, CharSet = CharSet.Auto)]
    static extern bool DeleteUrlCacheEntry(IntPtr lpszUrlName);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    struct INTERNET_CACHE_ENTRY_INFO
    {
        public uint dwStructSize;
        public IntPtr lpszSourceUrlName;
        public IntPtr lpszLocalFileName;
        public uint CacheEntryType;
        public uint dwUseCount;
        public uint dwHitRate;
        public uint dwSizeLow;
        public uint dwSizeHigh;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastModifiedTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME ExpireTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastAccessTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastSyncTime;
        public IntPtr lpHeaderInfo;
        public uint dwHeaderInfoSize;
        public IntPtr lpszFileExtension;
        public uint dwReserved;
    }

    const uint COOKIE_CACHE_ENTRY = 0x00100000;

    public static void EnableNoPersist()
    {
        var ptr = Marshal.AllocHGlobal(sizeof(int));
        Marshal.WriteInt32(ptr, INTERNET_SUPPRESS_COOKIE_PERSIST);
        InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SUPPRESS_BEHAVIOR, ptr, sizeof(int));
        Marshal.FreeHGlobal(ptr);
    }

    public static void ClearAll()
    {
        uint size = 0;
        IntPtr enumHandle = FindFirstUrlCacheEntry(null, IntPtr.Zero, ref size);
        if (enumHandle == IntPtr.Zero && size > 0)
        {
            IntPtr buffer = Marshal.AllocHGlobal((int)size);
            enumHandle = FindFirstUrlCacheEntry(null, buffer, ref size);
            if (enumHandle != IntPtr.Zero)
            {
                try
                {
                    while (true)
                    {
                        var info = Marshal.PtrToStructure<INTERNET_CACHE_ENTRY_INFO>(buffer);
                        if ((info.CacheEntryType & COOKIE_CACHE_ENTRY) == COOKIE_CACHE_ENTRY)
                        {
                            var url = info.lpszSourceUrlName;
                            if (url != IntPtr.Zero) DeleteUrlCacheEntry(url);
                        }
                        size = 0;
                        var ok = FindNextUrlCacheEntry(enumHandle, IntPtr.Zero, ref size);
                        if (!ok && size == 0) break;
                        Marshal.FreeHGlobal(buffer);
                        buffer = Marshal.AllocHGlobal((int)size);
                        ok = FindNextUrlCacheEntry(enumHandle, buffer, ref size);
                        if (!ok) break;
                    }
                }
                finally
                {
                    FindCloseUrlCache(enumHandle);
                }
            }
            Marshal.FreeHGlobal(buffer);
        }
    }
}