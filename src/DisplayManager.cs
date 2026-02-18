using System.Runtime.InteropServices;
using MonitorProfiles.Models;

namespace MonitorProfiles;

public static class DisplayManager
{
    #region Win32 Constants

    private const int CCHDEVICENAME = 32;
    private const int CCHFORMNAME = 32;
    private const uint ENUM_CURRENT_SETTINGS = 0xFFFFFFFF;
    private const uint DISPLAY_DEVICE_ATTACHED_TO_DESKTOP = 0x1;
    private const uint CDS_UPDATEREGISTRY = 0x01;
    private const uint CDS_NORESET = 0x10000000;
    private const uint CDS_SET_PRIMARY = 0x10;
    private const int DISP_CHANGE_SUCCESSFUL = 0;
    private const int DM_POSITION = 0x20;
    private const int DM_PELSWIDTH = 0x80000;
    private const int DM_PELSHEIGHT = 0x100000;
    private const int DM_DISPLAYORIENTATION = 0x80;

    #endregion

    #region Win32 Structs

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DISPLAY_DEVICE
    {
        public int cb;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceString;
        public uint StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceKey;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DEVMODE
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
        public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public int dmDisplayOrientation;
        public int dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
        public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;
    }

    #endregion

    #region P/Invoke

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool EnumDisplayDevices(
        string? lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool EnumDisplaySettingsEx(
        string lpszDeviceName, uint iModeNum, ref DEVMODE lpDevMode, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int ChangeDisplaySettingsEx(
        string? lpszDeviceName, ref DEVMODE lpDevMode, IntPtr hwnd, uint dwflags, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int ChangeDisplaySettingsEx(
        string? lpszDeviceName, IntPtr lpDevMode, IntPtr hwnd, uint dwflags, IntPtr lParam);

    #endregion

    public static List<DisplayInfo> GetCurrentDisplays()
    {
        var displays = new List<DisplayInfo>();

        var adapter = new DISPLAY_DEVICE();
        adapter.cb = Marshal.SizeOf(adapter);

        for (uint i = 0; EnumDisplayDevices(null, i, ref adapter, 0); i++)
        {
            if ((adapter.StateFlags & DISPLAY_DEVICE_ATTACHED_TO_DESKTOP) == 0)
            {
                adapter.cb = Marshal.SizeOf(adapter);
                continue;
            }

            // Get the monitor device for this adapter
            var monitor = new DISPLAY_DEVICE();
            monitor.cb = Marshal.SizeOf(monitor);
            EnumDisplayDevices(adapter.DeviceName, 0, ref monitor, 0);

            var devMode = new DEVMODE();
            devMode.dmSize = (short)Marshal.SizeOf(devMode);

            if (!EnumDisplaySettingsEx(adapter.DeviceName, ENUM_CURRENT_SETTINGS, ref devMode, 0))
            {
                adapter.cb = Marshal.SizeOf(adapter);
                continue;
            }

            displays.Add(new DisplayInfo
            {
                DeviceName = adapter.DeviceName,
                DeviceId = monitor.DeviceID ?? adapter.DeviceID ?? "",
                DeviceString = monitor.DeviceString ?? adapter.DeviceString ?? "",
                PositionX = devMode.dmPositionX,
                PositionY = devMode.dmPositionY,
                Width = devMode.dmPelsWidth,
                Height = devMode.dmPelsHeight,
                Orientation = devMode.dmDisplayOrientation,
                IsPrimary = devMode.dmPositionX == 0 && devMode.dmPositionY == 0
            });

            adapter.cb = Marshal.SizeOf(adapter);
        }

        return displays;
    }

    public static bool ApplyProfile(DisplayProfile profile)
    {
        var currentDisplays = GetCurrentDisplays();

        // Match profile displays to current displays by DeviceId
        var matched = new List<(DisplayInfo profileDisplay, DisplayInfo currentDisplay)>();

        foreach (var pd in profile.Displays)
        {
            var current = currentDisplays.FirstOrDefault(d => d.DeviceId == pd.DeviceId);
            if (current == null)
            {
                Console.WriteLine($"  Warning: Display '{pd.DeviceString}' (ID: {pd.DeviceId}) not found, skipping.");
                continue;
            }
            matched.Add((pd, current));
        }

        if (matched.Count == 0)
        {
            Console.WriteLine("Error: No matching displays found for this profile.");
            return false;
        }

        // Stage changes for each display
        foreach (var (pd, cd) in matched)
        {
            var devMode = new DEVMODE();
            devMode.dmSize = (short)Marshal.SizeOf(devMode);

            if (!EnumDisplaySettingsEx(cd.DeviceName, ENUM_CURRENT_SETTINGS, ref devMode, 0))
            {
                Console.WriteLine($"  Warning: Could not read settings for {cd.DeviceName}, skipping.");
                continue;
            }

            devMode.dmPositionX = pd.PositionX;
            devMode.dmPositionY = pd.PositionY;
            devMode.dmPelsWidth = pd.Width;
            devMode.dmPelsHeight = pd.Height;
            devMode.dmDisplayOrientation = pd.Orientation;
            devMode.dmFields = DM_POSITION | DM_PELSWIDTH | DM_PELSHEIGHT | DM_DISPLAYORIENTATION;

            uint flags = CDS_UPDATEREGISTRY | CDS_NORESET;
            if (pd.IsPrimary)
                flags |= CDS_SET_PRIMARY;

            int result = ChangeDisplaySettingsEx(cd.DeviceName, ref devMode, IntPtr.Zero, flags, IntPtr.Zero);
            if (result != DISP_CHANGE_SUCCESSFUL)
            {
                Console.WriteLine($"  Warning: ChangeDisplaySettingsEx failed for {cd.DeviceName} (error {result}).");
            }
        }

        // Apply all staged changes
        int applyResult = ChangeDisplaySettingsEx(null, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
        if (applyResult != DISP_CHANGE_SUCCESSFUL)
        {
            Console.WriteLine($"Error: Final apply failed (error {applyResult}).");
            return false;
        }

        return true;
    }
}
