using Microsoft.UI.Xaml;
using WinTouchpad.App.Localization;

namespace WinTouchpad.App;

public static class Program
{
    // Windows App SDK 1.7 — only for framework-dependent unpackaged runs (F5 / non-SC publish).
    // Self-contained publish must NOT call Bootstrap.Initialize; local CoreMessagingXP + Bootstrap = 0xc0000602.
    private const uint MajorMinorVersion = 0x00010007;

    internal static string[] Args { get; private set; } = Array.Empty<string>();

    [STAThread]
    private static void Main(string[] args)
    {
        Args = args ?? Array.Empty<string>();
        LogBoot("Program.Main enter");
        LocalizationBootstrap.ApplyFromOs();

#if WINDOWSAPPSDK_SELFCONTAINED
        LogBoot("skip Bootstrap (WindowsAppSDKSelfContained)");
#else
        try
        {
            Microsoft.Windows.ApplicationModel.DynamicDependency.Bootstrap.Initialize(MajorMinorVersion);
            LogBoot("Bootstrap.Initialize OK");
        }
        catch (Exception ex)
        {
            LogBoot("Bootstrap.Initialize FAIL: " + ex);
            throw;
        }
#endif

        WinRT.ComWrappersSupport.InitializeComWrappers();
        Application.Start(_ =>
        {
            var context = new Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(
                Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
            System.Threading.SynchronizationContext.SetSynchronizationContext(context);
            new App();
        });
    }

    private static void LogBoot(string message)
    {
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WinPrecisionTouchpad");
            Directory.CreateDirectory(dir);
            File.AppendAllText(
                Path.Combine(dir, "bootstrap.log"),
                $"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}");
        }
        catch
        {
            // ignore
        }
    }
}
