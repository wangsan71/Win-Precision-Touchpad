using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.UI;
using WinRT.Interop;

namespace WinTouchpad.App;

/// <summary>
/// Dark title-bar chrome so caption matches Win11 Settings-style dark content.
/// </summary>
internal static class WindowChromeHelper
{
    // Match LayerFill / typical Win11 dark surface (~#202020).
    private static readonly Color DarkBg = Color.FromArgb(255, 32, 32, 32);
    private static readonly Color DarkHover = Color.FromArgb(255, 45, 45, 45);
    private static readonly Color DarkPressed = Color.FromArgb(255, 58, 58, 58);
    private static readonly Color DarkInactive = Color.FromArgb(255, 32, 32, 32);

    public static void ApplyDarkTitleBar(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        if (window.Content is FrameworkElement root)
            root.RequestedTheme = ElementTheme.Dark;

        try
        {
            var titleBar = window.AppWindow.TitleBar;
            // Keep system caption buttons; tint non-client area dark (no content extend needed).
            titleBar.ExtendsContentIntoTitleBar = false;
            titleBar.BackgroundColor = DarkBg;
            titleBar.ForegroundColor = Colors.White;
            titleBar.InactiveBackgroundColor = DarkInactive;
            titleBar.InactiveForegroundColor = Color.FromArgb(255, 160, 160, 160);
            titleBar.ButtonBackgroundColor = DarkBg;
            titleBar.ButtonForegroundColor = Colors.White;
            titleBar.ButtonHoverBackgroundColor = DarkHover;
            titleBar.ButtonHoverForegroundColor = Colors.White;
            titleBar.ButtonPressedBackgroundColor = DarkPressed;
            titleBar.ButtonPressedForegroundColor = Colors.White;
            titleBar.ButtonInactiveBackgroundColor = DarkInactive;
            titleBar.ButtonInactiveForegroundColor = Color.FromArgb(255, 160, 160, 160);
        }
        catch
        {
            // Older builds / missing AppWindow title bar support — content theme still applied.
        }

        // Fallback for caption when TitleBar colors are ignored (some unpackaged hosts).
        try
        {
            var hwnd = WindowNative.GetWindowHandle(window);
            if (hwnd != IntPtr.Zero)
                NativeMethods.TrySetImmersiveDarkMode(hwnd, true);
        }
        catch
        {
            // best-effort
        }
    }

    private static class NativeMethods
    {
        private const int DwmwaUseImmersiveDarkMode = 20;
        private const int DwmwaUseImmersiveDarkModeBefore20H1 = 19;

        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        public static void TrySetImmersiveDarkMode(IntPtr hwnd, bool enabled)
        {
            int useDark = enabled ? 1 : 0;
            if (DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkMode, ref useDark, sizeof(int)) != 0)
                _ = DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkModeBefore20H1, ref useDark, sizeof(int));
        }
    }
}