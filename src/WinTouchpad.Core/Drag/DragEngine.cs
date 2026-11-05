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
    private bool _awaitingEstablishedSettle;
    private bool _leftButtonOutstanding;

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
        if (State == DragState.ReleasePending)
            return;

        if (!_options.Enabled)
        {
            if (State is DragState.Pending or DragState.Dragging)
                EndGesture();
            return;
        }

        if (!isThreeFinger)
        {
            if (State is DragState.Pending or DragState.Dragging)
                EndGesture();
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
                // Settle only needed when the third contact just appeared (IsNew).
                // If all three are already established, accumulate immediately (more responsive).
                _awaitingEstablishedSettle = established < 3 || HasNew(deltas);
                break;

            case DragState.Pending:
                if (established < 3)
                {
                    // Still acquiring the third finger — ignore settle noise.
                    _awaitingEstablishedSettle = true;
                    break;
                }

                if (_awaitingEstablishedSettle)
                {
                    // Drop one post-establish frame (contact ID rematch / coordinate settle).
                    _awaitingEstablishedSettle = false;
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
        if (_leftButtonOutstanding || State is DragState.Pending or DragState.Dragging or DragState.ReleasePending)
            EndGesture();
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
        _leftButtonOutstanding = true;
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

    private void EndGesture()
    {
        if (_leftButtonOutstanding)
        {
            try
            {
                _mouse.LeftUp();
                _leftButtonOutstanding = false;
            }
            catch
            {
                ClearPendingMovement();
                TransitionTo(DragState.ReleasePending);
                return;
            }
        }

        ClearPendingMovement();
        TransitionTo(DragState.End);
        TransitionTo(DragState.Idle);
    }

    private void ClearPendingMovement()
    {
        _pendingAccumX = 0;
        _pendingAccumY = 0;
        _pendingDistance = 0;
        _awaitingEstablishedSettle = false;
    }

    private void TransitionTo(DragState next)
    {
        if (State == next) return;
        var prev = State;
        State = next;
        try
        {
            StateChanged?.Invoke(prev, next);
        }
        catch
        {
            // State observers are diagnostic only and must not interrupt input recovery.
        }
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

    private static bool HasNew(IReadOnlyList<ContactDelta> deltas)
    {
        foreach (var d in deltas)
        {
            if (d.IsNew) return true;
        }
        return false;
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
