using System.Diagnostics;

namespace WinTouchpad.Core.Input;

/// <summary>
/// Briefly raises process priority while three-finger dragging so the Raw Input → SendInput
/// path is less likely to stall under Windows power throttling (e.g. ≤20W laptop limits).
/// Restores the previous class on idle. Best-effort; failures are ignored.
/// </summary>
public static class DragProcessBoost
{
    private static ProcessPriorityClass? _previous;
    private static int _active;

    public static void SetDragging(bool dragging)
    {
        try
        {
            if (dragging)
            {
                if (Interlocked.Exchange(ref _active, 1) == 1)
                    return;

                var proc = Process.GetCurrentProcess();
                _previous = proc.PriorityClass;
                if (proc.PriorityClass is ProcessPriorityClass.Normal or ProcessPriorityClass.BelowNormal or ProcessPriorityClass.Idle)
                    proc.PriorityClass = ProcessPriorityClass.AboveNormal;
            }
            else
            {
                if (Interlocked.Exchange(ref _active, 0) == 0)
                    return;

                if (_previous is { } prev)
                {
                    Process.GetCurrentProcess().PriorityClass = prev;
                    _previous = null;
                }
            }
        }
        catch
        {
            // Priority changes are optional; never break drag.
        }
    }
}
