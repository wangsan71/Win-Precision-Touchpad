using WinTouchpad.Core.Contacts;
using WinTouchpad.Core.Drag;
using Xunit;

namespace WinTouchpad.Core.Tests;

public class DragEngineTests
{
    private static ContactDelta Delta(int id, int dx, int dy, bool isNew = false) =>
        new(id, dx, dy, x: 0, y: 0, isNew);

    private static IReadOnlyList<ContactDelta> ThreeMove(int dx, int dy) =>
        new[] { Delta(1, dx, dy), Delta(2, dx, dy), Delta(3, dx, dy) };

    private static IReadOnlyList<ContactDelta> ThreeNew() =>
        new[] { Delta(1, 0, 0, isNew: true), Delta(2, 0, 0, isNew: true), Delta(3, 0, 0, isNew: true) };

    /// <summary>Idle→Pending (no accumulate), then accumulate until threshold.</summary>
    private static void ArmPending(DragEngine engine)
    {
        engine.Process(true, ThreeMove(0, 0));
        Assert.Equal(DragState.Pending, engine.State);
    }

    [Fact]
    public void StaysPending_UntilThreshold_ThenLeftDown()
    {
        var mouse = new RecordingMouseInjector();
        var engine = new DragEngine(mouse, new DragEngineOptions { StartDistanceThreshold = 10f, Sensitivity = 1f });

        var transitions = new List<(DragState from, DragState to)>();
        engine.StateChanged += (a, b) => transitions.Add((a, b));

        ArmPending(engine);
        Assert.Equal(0, mouse.LeftDownCount);

        // Under threshold (3+3=6 < 10)
        engine.Process(true, ThreeMove(3, 0));
        engine.Process(true, ThreeMove(3, 0));
        Assert.Equal(DragState.Pending, engine.State);
        Assert.Equal(0, mouse.LeftDownCount);

        // Cross threshold (6+5=11 >= 10) → Dragging + Down + flush move
        engine.Process(true, ThreeMove(5, 0));
        Assert.Equal(DragState.Dragging, engine.State);
        Assert.Equal(1, mouse.LeftDownCount);
        Assert.Contains(transitions, t => t.from == DragState.Idle && t.to == DragState.Pending);
        Assert.Contains(transitions, t => t.from == DragState.Pending && t.to == DragState.Dragging);
        Assert.True(mouse.TotalDx > 0);
    }

    [Fact]
    public void Dragging_MovesCursor_ThenLift_EndsWithUp()
    {
        var mouse = new RecordingMouseInjector();
        var engine = new DragEngine(mouse, new DragEngineOptions { StartDistanceThreshold = 5f, Sensitivity = 2f });

        ArmPending(engine);
        engine.Process(true, ThreeMove(5, 0)); // crosses threshold; flush Move(10,0) at sens=2
        Assert.Equal(DragState.Dragging, engine.State);
        Assert.Equal(1, mouse.LeftDownCount);
        Assert.Equal(10f, mouse.TotalDx, 2);

        engine.Process(true, ThreeMove(4, -2));
        Assert.Equal(DragState.Dragging, engine.State);
        // cumulative: flush 10 + move (4*2, -2*2) = 18, -4
        Assert.Equal(18f, mouse.TotalDx, 2);
        Assert.Equal(-4f, mouse.TotalDy, 2);

        engine.Process(false, Array.Empty<ContactDelta>());
        Assert.Equal(DragState.Idle, engine.State);
        Assert.Equal(1, mouse.LeftUpCount);
        Assert.Contains("Up", mouse.Actions);
    }

    [Fact]
    public void Disabled_DoesNotInject_AndResetsPending()
    {
        var mouse = new RecordingMouseInjector();
        var opts = new DragEngineOptions { StartDistanceThreshold = 5f, Enabled = false };
        var engine = new DragEngine(mouse, opts);

        engine.Process(true, ThreeMove(20, 0));
        Assert.Equal(DragState.Idle, engine.State);
        Assert.Empty(mouse.Actions);

        opts.Enabled = true;
        ArmPending(engine);
        engine.Process(true, ThreeMove(20, 0));
        Assert.Equal(DragState.Dragging, engine.State);
        Assert.Equal(1, mouse.LeftDownCount);

        opts.Enabled = false;
        engine.Process(true, ThreeMove(1, 0));
        Assert.Equal(DragState.Idle, engine.State);
        Assert.Equal(1, mouse.LeftUpCount);
    }

    [Fact]
    public void NewContactsOnly_DoNotMove()
    {
        var mouse = new RecordingMouseInjector();
        var engine = new DragEngine(mouse, new DragEngineOptions { StartDistanceThreshold = 1f });

        ArmPending(engine);
        engine.Process(true, ThreeMove(5, 0));
        Assert.Equal(DragState.Dragging, engine.State);
        var movesBefore = mouse.Actions.Count(a => a.StartsWith("Move"));

        engine.Process(true, ThreeNew());
        var movesAfter = mouse.Actions.Count(a => a.StartsWith("Move"));
        Assert.Equal(movesBefore, movesAfter);
    }

    [Fact]
    public void PendingLift_WithoutThreshold_NoClick()
    {
        var mouse = new RecordingMouseInjector();
        var engine = new DragEngine(mouse, new DragEngineOptions { StartDistanceThreshold = 100f });

        ArmPending(engine);
        engine.Process(true, ThreeMove(2, 0));
        Assert.Equal(DragState.Pending, engine.State);

        engine.Process(false, Array.Empty<ContactDelta>());
        Assert.Equal(DragState.Idle, engine.State);
        Assert.Equal(0, mouse.LeftDownCount);
        Assert.Equal(0, mouse.LeftUpCount);
    }

    [Fact]
    public void FourFingerRoute_EndsActiveDrag()
    {
        var mouse = new RecordingMouseInjector();
        var engine = new DragEngine(mouse, new DragEngineOptions { StartDistanceThreshold = 1f });

        ArmPending(engine);
        engine.Process(true, ThreeMove(10, 0));
        Assert.Equal(DragState.Dragging, engine.State);

        // Router would send isThreeFinger=false for 4-finger
        engine.Process(false, ThreeMove(1, 0));
        Assert.Equal(DragState.Idle, engine.State);
        Assert.Equal(1, mouse.LeftUpCount);
    }

    [Fact]
    public void NewContacts_RequireOneSettleFrame_BeforeAccumulate()
    {
        var mouse = new RecordingMouseInjector();
        var engine = new DragEngine(mouse, new DragEngineOptions { StartDistanceThreshold = 5f, Sensitivity = 1f });

        // Landing with IsNew → Pending + settle armed
        engine.Process(true, ThreeNew());
        Assert.Equal(DragState.Pending, engine.State);

        // First established frame is dropped (settle)
        engine.Process(true, ThreeMove(10, 0));
        Assert.Equal(DragState.Pending, engine.State);
        Assert.Equal(0, mouse.LeftDownCount);

        // Next frame accumulates and can begin drag
        engine.Process(true, ThreeMove(10, 0));
        Assert.Equal(DragState.Dragging, engine.State);
        Assert.Equal(1, mouse.LeftDownCount);
    }
}
