using Microsoft.UI.Xaml;

namespace WinTouchpad.App;

/// <summary>Hidden lifetime host window (tray app stays resident).</summary>
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Title = "Win Precision Touchpad";
    }
}