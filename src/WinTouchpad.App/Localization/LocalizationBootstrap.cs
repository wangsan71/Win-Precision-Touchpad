using System.Globalization;

namespace WinTouchpad.App.Localization;

/// <summary>
/// Resolve UI culture from Windows (CurrentUICulture), with Traditional Chinese fallback.
/// Supported: zh-TW / zh-HK / zh (Traditional default), zh-CN (Simplified), en-US / en-GB / en.
/// </summary>
internal static class LocalizationBootstrap
{
    public static void ApplyFromOs()
    {
        var ui = CultureInfo.CurrentUICulture;
        var chosen = Resolve(ui);
        UiStrings.SetCulture(chosen);
    }

    internal static CultureInfo Resolve(CultureInfo ui)
    {
        // Exact / parent match against shipped satellites.
        for (var c = ui; !Equals(c, CultureInfo.InvariantCulture); c = c.Parent)
        {
            var name = c.Name;
            if (string.Equals(name, "zh-CN", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "zh-Hans", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "zh-SG", StringComparison.OrdinalIgnoreCase))
                return CultureInfo.GetCultureInfo("zh-CN");

            if (string.Equals(name, "zh-TW", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "zh-HK", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "zh-MO", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "zh-Hant", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "zh", StringComparison.OrdinalIgnoreCase))
                return CultureInfo.GetCultureInfo("zh-TW");

            if (string.Equals(name, "en-GB", StringComparison.OrdinalIgnoreCase))
                return CultureInfo.GetCultureInfo("en-GB");

            if (string.Equals(name, "en-US", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "en", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("en-", StringComparison.OrdinalIgnoreCase))
                return CultureInfo.GetCultureInfo("en-US");
        }

        // Unknown UI language → Traditional Chinese default.
        return CultureInfo.GetCultureInfo("zh-TW");
    }
}
