using WinTouchpad.Core.Input;

namespace WinTouchpad.Core.Tests;

internal sealed class RecordingMouseInjector : IMouseInjector
{
    public List<string> Actions { get; } = new();
    public float TotalDx { get; private set; }
    public float TotalDy { get; private set; }
    public int LeftDownCount { get; private set; }
    public int LeftUpCount { get; private set; }
    public int LeftUpFailuresRemaining { get; set; }

    public void LeftDown()
    {
        LeftDownCount++;
        Actions.Add("Down");
    }

    public void LeftUp()
    {
        LeftUpCount++;
        if (LeftUpFailuresRemaining > 0)
        {
            LeftUpFailuresRemaining--;
            throw new InvalidOperationException("Simulated LeftUp failure.");
        }
        Actions.Add("Up");
    }

    public void MoveRelative(float dx, float dy)
    {
        TotalDx += dx;
        TotalDy += dy;
        Actions.Add($"Move({dx:0.##},{dy:0.##})");
    }
}