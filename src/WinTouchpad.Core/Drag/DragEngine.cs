using WinTouchpad.Core.Contacts;
using WinTouchpad.Core.Input;

namespace WinTouchpad.Core.Drag;

/// <summary>
/// Three-finger drag state machine: Idle → Pending → Dragging → End → Idle.
/// Uses average contact deltas + SendInput left-button drag (macOS Three Finger Drag style).
/// </summary>
public sealed class DragEngine
{
    private readonly IMouseInjector _mouse;
    private readonly DragEngineOptions _options;

    private float _pendingAccumX;
    private float _pendingAccumY;
    private float _pendingDistance;
    private bool _skipNextDeltaAfterArmed;

    public DragEngine(IMouseInjector mouse, DragEngineOptions? options = null)
    {
        _mouse = mouse ?? throw new ArgumentNullException(nameof(mouse));
        _options = options ?? new DragEngineOptions();
    }

    public DragEngineOptions Options => _options;
    public DragState State { get; private set; } = DragState.Idle;

    /// <summary>Raised after every state transition (including End before Idle).</summary>
    public event Action<DragState, DragState>? StateChanged;

    /// <summary>
    /// Process one frame for the three-finger pipeline.
    /// <paramref name="isThreeFinger"/> should be true only when FingerCountRouter says ThreeFinger.
    /// </summary>
    public void Process(bool isThreeFinger, IReadOnlyList<ContactDelta> deltas)
    {
        if (!_options.Enabled)
        {
            if (State is DragState.Pending or DragState.Dragging)
                EndGesture(releaseButton: State == DragState.Dragging);
            return;
        }

        if (!isThreeFinger)
        {
            if (State is DragState.Pending or DragState.Dragging)
                EndGesture(releaseButton: State == DragState.Dragging);
            return;
        }

        // Only accumulate / begin drag once all three contacts are established.
        // Landing jitter on newly-appeared contacts used to trip StartDistanceThreshold
        // and fire a brief LeftDown/LeftUp "flash" with no intentional move.
        var established = CountEstablished(deltas);
        var (avgDx, avgDy, hasMove) = AverageDelta(deltas);

        switch (State)
        {
            case DragState.Idle:
                TransitionTo(DragState.Pending);
                _pendingAccumX = 0;
                _pendingAccumY = 0;
                _pendingDistance = 0;
                _skipNextDeltaAfterArmed = true;
                // Wait until three contacts are tracked before measuring movement.
                break;

            case DragState.Pending:
                if (established < 3)
                {
                    // Still acquiring the third finger — ignore settle noise.
                    _skipNextDeltaAfterArmed = true;
                    break;
                }

                if (_skipNextDeltaAfterArmed)
                {
                    // Drop one post-arm frame (contact ID rematch / coordinate settle).
                    _skipNextDeltaAfterArmed = false;
                    break;
                }

                if (hasMove)
                    AccumulatePending(avgDx, avgDy);
                if (_pendingDistance >= _options.StartDistanceThreshold)
                    BeginDrag();
                break;

            case DragState.Dragging:
                if (hasMove)
                {
                    var sx = avgDx * _options.Sensitivity;
                    var sy = avgDy * _options.Sensitivity;
                    if (sx != 0 || sy != 0)
                        _mouse.MoveRelative(sx, sy);
                }
                break;
        }
    }

    /// <summary>Force end (e.g. app exit). Safe if already Idle.</summary>
    public void Reset()
    {
        if (State is DragState.Pending or DragState.Dragging)
            EndGesture(releaseButton: State == DragState.Dragging);
        else if (State != DragState.Idle)
            TransitionTo(DragState.Idle);
    }

    private void AccumulatePending(float dx, float dy)
    {
        _pendingAccumX += dx;
        _pendingAccumY += dy;
        _pendingDistance = MathF.Sqrt(_pendingAccumX * _pendingAccumX + _pendingAccumY * _pendingAccumY);
    }

    private void BeginDrag()
    {
        _mouse.LeftDown();
        TransitionTo(DragState.Dragging);
        // Flush pending movement so the cursor catches up after the threshold.
        var sx = _pendingAccumX * _options.Sensitivity;
        var sy = _pendingAccumY * _options.Sensitivity;
        _pendingAccumX = 0;
        _pendingAccumY = 0;
        _pendingDistance = 0;
        if (sx != 0 || sy != 0)
            _mouse.MoveRelative(sx, sy);
    }

    private void EndGesture(bool releaseButton)
    {
        if (releaseButton)
        {
            try { _mouse.LeftUp(); }
            catch { /* best-effort release */ }
        }

        _pendingAccumX = 0;
        _pendingAccumY = 0;
        _pendingDistance = 0;
        _skipNextDeltaAfterArmed = false;
        TransitionTo(DragState.End);
        TransitionTo(DragState.Idle);
    }

    private void TransitionTo(DragState next)
    {
        if (State == next) return;
        var prev = State;
        State = next;
        StateChanged?.Invoke(prev, next);
    }

    private static int CountEstablished(IReadOnlyList<ContactDelta> deltas)
    {
        var n = 0;
        foreach (var d in deltas)
        {
            if (!d.IsNew) n++;
        }
        return n;
    }

    private static (float avgDx, float avgDy, bool hasMove) AverageDelta(IReadOnlyList<ContactDelta> deltas)
    {
        if (deltas.Count == 0)
            return (0, 0, false);

        float sumDx = 0, sumDy = 0;
        int n = 0;
        foreach (var d in deltas)
        {
            if (d.IsNew) continue;
            sumDx += d.Dx;
            sumDy += d.Dy;
            n++;
        }

        if (n == 0)
            return (0, 0, false);

        var avgDx = sumDx / n;
        var avgDy = sumDy / n;
        return (avgDx, avgDy, avgDx != 0 || avgDy != 0);
    }
}