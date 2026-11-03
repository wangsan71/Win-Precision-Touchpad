using System.Collections.Concurrent;
using System.Text;

namespace WinTouchpad.Core.Logging;

/// <summary>
/// Thread-safe logger that writes a rolling file under LocalAppData and keeps recent lines for UI.
/// INFO lines are buffered (no AutoFlush) so hot-path logging cannot stall Raw Input → SendInput
/// under disk/power throttle; WARN/ERROR still flush immediately.
/// </summary>
public sealed class AppLogger : IAppLogger, IDisposable
{
    private readonly object _gate = new();
    private readonly ConcurrentQueue<string> _recent = new();
    private readonly int _recentCapacity;
    private readonly string _logDirectory;
    private readonly string _logFilePath;
    private StreamWriter? _writer;
    private bool _disposed;
    private int _infoSinceFlush;
    private long _lastFlushTicks;

    public AppLogger(string? logDirectory = null, int recentCapacity = 500)
    {
        _recentCapacity = Math.Max(50, recentCapacity);
        _logDirectory = logDirectory
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WinPrecisionTouchpad");
        Directory.CreateDirectory(_logDirectory);
        _logFilePath = Path.Combine(_logDirectory, $"touchpad-{DateTime.Now:yyyyMMdd}.log");
        _writer = new StreamWriter(
            new FileStream(_logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite),
            Encoding.UTF8)
        {
            AutoFlush = false
        };
        _lastFlushTicks = Environment.TickCount64;
    }

    public string LogDirectory => _logDirectory;
    public string LogFilePath => _logFilePath;

    public event Action<string>? LineWritten;

    public IReadOnlyList<string> GetRecentLines() => _recent.ToArray();

    public void Info(string message) => Write("INFO", message, flush: false);
    public void Warn(string message) => Write("WARN", message, flush: true);
    public void Error(string message, Exception? ex = null)
    {
        var text = ex is null ? message : $"{message} | {ex.GetType().Name}: {ex.Message}";
        Write("ERROR", text, flush: true);
    }

    private void Write(string level, string message, bool flush)
    {
        if (_disposed) return;
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {level} {message}";
        lock (_gate)
        {
            _writer?.WriteLine(line);
            if (flush)
            {
                _writer?.Flush();
                _infoSinceFlush = 0;
                _lastFlushTicks = Environment.TickCount64;
            }
            else
            {
                _infoSinceFlush++;
                // Bound how long INFO can sit unflushed without syncing every line.
                if (_infoSinceFlush >= 32 || Environment.TickCount64 - _lastFlushTicks >= 250)
                {
                    _writer?.Flush();
                    _infoSinceFlush = 0;
                    _lastFlushTicks = Environment.TickCount64;
                }
            }
        }

        _recent.Enqueue(line);
        while (_recent.Count > _recentCapacity && _recent.TryDequeue(out _)) { }

        try { LineWritten?.Invoke(line); }
        catch { /* UI subscribers must not break logging */ }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        lock (_gate)
        {
            try { _writer?.Flush(); } catch { /* ignore */ }
            _writer?.Dispose();
            _writer = null;
        }
    }
}
