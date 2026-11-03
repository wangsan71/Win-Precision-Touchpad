using WinTouchpad.Core.Settings;
using Xunit;

namespace WinTouchpad.Core.Tests;

public class SettingsStoreTests
{
    [Fact]
    public void Load_MissingFile_ReturnsDefaults()
    {
        var dir = CreateTempDir();
        try
        {
            var store = new SettingsStore(dir);
            var s = store.Load();
            Assert.True(s.DragEnabled);
            Assert.Equal(0.6f, s.Sensitivity, 3);
            Assert.Equal(12f, s.StartDistanceThreshold, 3);
            Assert.False(s.StartWithWindows);
            Assert.Equal(AppSettings.CurrentVersion, s.SettingsVersion);
        }
        finally
        {
            TryDelete(dir);
        }
    }

    [Fact]
    public void Save_ThenLoad_RoundTrips()
    {
        var dir = CreateTempDir();
        try
        {
            var store = new SettingsStore(dir);
            var original = new AppSettings
            {
                DragEnabled = false,
                Sensitivity = 0.45f,
                StartDistanceThreshold = 18f,
                StartWithWindows = true
            };
            store.Save(original);

            Assert.True(File.Exists(store.FilePath));

            var loaded = store.Load();
            Assert.False(loaded.DragEnabled);
            Assert.Equal(0.45f, loaded.Sensitivity, 3);
            Assert.Equal(18f, loaded.StartDistanceThreshold, 3);
            Assert.True(loaded.StartWithWindows);
            Assert.Equal(AppSettings.CurrentVersion, loaded.SettingsVersion);
        }
        finally
        {
            TryDelete(dir);
        }
    }

    [Fact]
    public void Clamp_LimitsSensitivityAndThreshold()
    {
        var s = new AppSettings { Sensitivity = 9f, StartDistanceThreshold = -5f };
        s.Clamp();
        Assert.Equal(2.0f, s.Sensitivity, 3);
        Assert.Equal(0f, s.StartDistanceThreshold, 3);

        s.Sensitivity = 0.05f;
        s.StartDistanceThreshold = 500f;
        s.Clamp();
        Assert.Equal(0.2f, s.Sensitivity, 3);
        Assert.Equal(200f, s.StartDistanceThreshold, 3);
    }

    [Fact]
    public void Load_CorruptJson_ReturnsDefaults()
    {
        var dir = CreateTempDir();
        try
        {
            var store = new SettingsStore(dir);
            File.WriteAllText(store.FilePath, "{ not json");
            var s = store.Load();
            Assert.Equal(0.6f, s.Sensitivity, 3);
            Assert.True(s.DragEnabled);
        }
        finally
        {
            TryDelete(dir);
        }
    }

    [Fact]
    public void Clone_CopiesFields()
    {
        var a = new AppSettings { DragEnabled = false, Sensitivity = 1.2f, StartWithWindows = true };
        var b = a.Clone();
        Assert.False(b.DragEnabled);
        Assert.Equal(1.2f, b.Sensitivity, 3);
        Assert.True(b.StartWithWindows);
        b.Sensitivity = 0.3f;
        Assert.Equal(1.2f, a.Sensitivity, 3);
    }

    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "WinTouchpadSettingsTest-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static void TryDelete(string dir)
    {
        try { Directory.Delete(dir, recursive: true); }
        catch { /* best effort */ }
    }
}