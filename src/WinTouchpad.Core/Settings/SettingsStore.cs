using System.Text;
using System.Text.Json;

namespace WinTouchpad.Core.Settings;

/// <summary>
/// Loads and saves <see cref="AppSettings"/> as JSON under
/// <c>%LocalAppData%\WinPrecisionTouchpad\settings.json</c>.
/// </summary>
public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly string _directory;
    private readonly string _filePath;

    /// <summary>
    /// Creates a store. When <paramref name="directory"/> is null, uses
    /// <c>%LocalAppData%\WinPrecisionTouchpad</c>.
    /// </summary>
    public SettingsStore(string? directory = null)
    {
        _directory = directory
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WinPrecisionTouchpad");
        _filePath = Path.Combine(_directory, "settings.json");
    }

    /// <summary>Directory that holds settings.json (and logs).</summary>
    public string DirectoryPath => _directory;

    /// <summary>Full path to settings.json.</summary>
    public string FilePath => _filePath;

    /// <summary>
    /// Loads settings, migrating older versions when needed.
    /// Missing or corrupt files yield defaults (and do not throw).
    /// </summary>
    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_filePath))
                return AppSettings.CreateDefault();

            var json = File.ReadAllText(_filePath, Encoding.UTF8);
            var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            if (loaded is null)
                return AppSettings.CreateDefault();

            Migrate(loaded);
            loaded.Clamp();
            return loaded;
        }
        catch
        {
            return AppSettings.CreateDefault();
        }
    }

    /// <summary>Writes settings (clamped, version stamped). Creates the directory if needed.</summary>
    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Clamp();
        settings.SettingsVersion = AppSettings.CurrentVersion;

        Directory.CreateDirectory(_directory);
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        var temp = _filePath + ".tmp";
        File.WriteAllText(temp, json, Encoding.UTF8);
        File.Copy(temp, _filePath, overwrite: true);
        File.Delete(temp);
    }

    /// <summary>
    /// Upgrade path for older settings files. v1 is the first schema; future versions
    /// add fields here and bump <see cref="AppSettings.CurrentVersion"/>.
    /// </summary>
    private static void Migrate(AppSettings settings)
    {
        if (settings.SettingsVersion < 1)
            settings.SettingsVersion = 1;

        // Future: if (settings.SettingsVersion < 2) { ...; settings.SettingsVersion = 2; }
    }
}
