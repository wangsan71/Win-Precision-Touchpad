namespace WinTouchpad.Core.Input;

/// <summary>
/// Abstraction over mouse injection so DragEngine can be unit-tested without SendInput.
/// </summary>
public interface IMouseInjector
{
    void LeftDown();
    void LeftUp();
    /// <summary>Relative cursor move in screen pixels (sub-pixel residuals may be kept by the implementation).</summary>
    void MoveRelative(float dx, float dy);
}