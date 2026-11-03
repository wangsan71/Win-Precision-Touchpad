namespace WinTouchpad.Core.Logging;

/// <summary>Minimal logging surface used by Core and Tray.</summary>
public interface IAppLogger
{
    void Info(string message);
    void Warn(string message);
    void Error(string message, Exception? ex = null);
}