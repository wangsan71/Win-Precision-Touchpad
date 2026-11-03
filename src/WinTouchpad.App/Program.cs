using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.DynamicDependency;
using WinTouchpad.App.Localization;

namespace WinTouchpad.App;

public static class Program
{
    // Windows App SDK 1.7 — unpackaged apps must bootstrap the framework package
    // so XamlControlsResources can resolve ms-appx:///Microsoft.UI.Xaml/Themes/*.
    private const uint MajorMinorVersion = 0x00010007;

    internal static string[] Args { get; private set; } = Array.Empty<string>();

    [STAThread]
    private static void Main(string[] args)
    {
        Args = args ?? Array.Empty<string>();
        LocalizationBootstrap.ApplyFromOs();
        Bootstrap.Initialize(MajorMinorVersion);

        WinRT.ComWrappersSupport.InitializeComWrappers();
        Application.Start(_ =>
        {
            var context = new Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(
                Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
            System.Threading.SynchronizationContext.SetSynchronizationContext(context);
            new App();
        });
    }
}
