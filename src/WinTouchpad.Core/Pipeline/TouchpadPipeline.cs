using WinTouchpad.Core.Contacts;
using WinTouchpad.Core.Drag;
using WinTouchpad.Core.Input;
using WinTouchpad.Core.Logging;
using WinTouchpad.Core.Routing;
using WinTouchpad.Core.Settings;

namespace WinTouchpad.Core.Pipeline;

/// <summary>
/// Orchestrates Raw Input frames: route by finger count, track deltas, feed three-finger DragEngine.
/// </summary>
public sealed class TouchpadPipeline
{
    private readonly FingerCountRouter _router = new();
    private readonly ContactTracker _tracker = new();
    private readonly DragEngine _drag;
    private readonly IAppLogger _logger;
    private int _lastLoggedFingerCount = -1;
    private DateTime _lastVerboseLogUtc = DateTime.MinValue;

    /// <summary>
    /// Minimum interval between verbose per-move log lines when not actively dragging.
    /// While Dragging, per-move logs are suppressed so AutoFlush disk I/O cannot stall SendInput
    /// on the Raw Input thread (especially under ≤20W power limits).
    /// </summary>
    public TimeSpan VerboseLogInterval { get; set; } = TimeSpan.FromMilliseconds(250);

    public TouchpadPipeline(IAppLogger logger, IMouseInjector? mouse = null, DragEngineOptions? dragOptions = null)
    {
        _logger = logger;
        _drag = new DragEngine(mouse ?? new MouseInjector(), dragOptions);
        _drag.StateChanged += OnDragStateChanged;
    }

    public FingerCountRouter Router => _router;
    public ContactTracker Tracker => _tracker;
    public DragEngine Drag => _drag;

    /// <summary>Handle one parsed Raw Input frame.</summary>
    public void HandleFrame(ContactFrame frame)
    {
        var route = _router.Route(frame);
        var deltas = _tracker.Update(frame.Contacts);

        if (route != FingerRoute.ThreeFinger && frame.FingerCount != _lastLoggedFingerCount)
        {
            if (route == FingerRoute.FourFingerReserved)
            {
                SafeInfo($"route=FourFingerReserved (v1 ignore) contacts={frame.FingerCount} [{FormatContacts(frame.Contacts)}]");
            }
            else if (route is FingerRoute.None or FingerRoute.IgnoredLowCount or FingerRoute.IgnoredHighCount)
            {
                SafeInfo($"route={route} contacts={frame.FingerCount}");
            }

            _lastLoggedFingerCount = frame.FingerCount;
        }

        // Avoid disk/UI log work on the hot Dragging path — that was a continuous lag source.
        if (route == FingerRoute.ThreeFinger && _drag.State != DragState.Dragging)
        {
            var now = DateTime.UtcNow;
            if (now - _lastVerboseLogUtc >= VerboseLogInterval || frame.FingerCount != _lastLoggedFingerCount)
            {
                _lastVerboseLogUtc = now;
                _lastLoggedFingerCount = frame.FingerCount;
                var deltaText = string.Join(" ", deltas.Select(d => d.ToString()));
                SafeInfo($"contacts={frame.FingerCount} reported={frame.ReportedContactCount} {FormatContacts(frame.Contacts)} | {deltaText}");
            }
        }

        // 3-finger pipeline only; 4-finger stays reserved (no injection).
        try
        {
            _drag.Process(isThreeFinger: route == FingerRoute.ThreeFinger, deltas);
        }
        catch (Exception ex)
        {
            try
            {
                _drag.Reset();
            }
            finally
            {
                SafeError("DragEngine.Process failed", ex);
            }
        }

        if (frame.FingerCount == 0)
        {
            _tracker.Reset();
            _lastLoggedFingerCount = 0;
        }
    }

    /// <summary>Copy drag-related fields from persisted settings onto the live engine options.</summary>
    public void ApplySettings(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Clamp();
        _drag.Options.Enabled = settings.DragEnabled;
        _drag.Options.Sensitivity = settings.Sensitivity;
        _drag.Options.StartDistanceThreshold = settings.StartDistanceThreshold;
        if (!settings.DragEnabled)
            _drag.Reset();
        SafeInfo($"settings applied: enabled={settings.DragEnabled} sensitivity={settings.Sensitivity:0.##} startDistance={settings.StartDistanceThreshold:0.##}");
    }

    public void SetDragEnabled(bool enabled)
    {
        _drag.Options.Enabled = enabled;
        if (!enabled)
            _drag.Reset();
        SafeInfo($"drag.enabled={enabled}");
    }

    private void OnDragStateChanged(DragState previous, DragState next)
    {
        SafeInfo($"drag.state {previous} → {next}");
        DragProcessBoost.SetDragging(next == DragState.Dragging);
    }

    private void SafeInfo(string message)
    {
        try { _logger.Info(message); }
        catch { /* Logging must not interrupt input processing. */ }
    }

    private void SafeError(string message, Exception exception)
    {
        try { _logger.Error(message, exception); }
        catch { /* Logging must not interrupt recovery. */ }
    }

    private static string FormatContacts(IReadOnlyList<TouchpadContact> contacts) =>
        string.Join("; ", contacts.Select(c => c.ToString()));
}
