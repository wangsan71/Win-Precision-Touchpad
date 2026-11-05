using WinTouchpad.Core.Contacts;
using WinTouchpad.Core.Drag;
using WinTouchpad.Core.Logging;
using WinTouchpad.Core.Pipeline;
using WinTouchpad.Core.Settings;
using Xunit;

namespace WinTouchpad.Core.Tests;

public class TouchpadPipelineLoggingTests
{
    [Fact]
    public void LoggerFailure_DoesNotPreventDragRelease()
    {
        var mouse = new RecordingMouseInjector();
        var pipeline = new TouchpadPipeline(new ThrowingLogger(), mouse, new DragEngineOptions
        {
            StartDistanceThreshold = 1f,
            Sensitivity = 1f
        });
        pipeline.ApplySettings(new AppSettings { DragEnabled = true, StartDistanceThreshold = 1f });

        pipeline.HandleFrame(Frame(0, 3, (1, 0), (2, 0), (3, 0)));
        pipeline.HandleFrame(Frame(1, 3, (1, 0), (2, 0), (3, 0)));
        pipeline.HandleFrame(Frame(2, 3, (1, 0), (2, 0), (3, 0)));
        Assert.Equal(DragState.Dragging, pipeline.Drag.State);
        Assert.Equal(1, mouse.LeftDownCount);

        pipeline.HandleFrame(Frame(3, 0));

        Assert.Equal(DragState.Idle, pipeline.Drag.State);
        Assert.Equal(1, mouse.LeftUpCount);
    }

    private static ContactFrame Frame(int tick, uint count, params (int Id, int X)[] contacts)
    {
        var parsed = contacts.Select(c => new TouchpadContact(c.Id, c.X + tick * 2, 100)).ToArray();
        return new ContactFrame(parsed, count, IntPtr.Zero);
    }

    private sealed class ThrowingLogger : IAppLogger
    {
        public void Info(string message) => throw new IOException("Simulated log write failure.");
        public void Warn(string message) => throw new IOException("Simulated log write failure.");
        public void Error(string message, Exception? ex = null) => throw new IOException("Simulated log write failure.");
    }
}
