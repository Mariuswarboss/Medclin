using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;

namespace Mediclin.UI.Services;

/// <summary>
/// Serviciu static pentru schimbarea temei (Light / Dark) la runtime.
/// Preferința se salvează în %AppData%\MediClin\theme.json.
/// </summary>
public static class ThemeService
{
    private const string LightSource = "Resources/Colors.xaml";
    private const string DarkSource  = "Resources/Colors.Dark.xaml";

    private static readonly string PrefFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MediClin", "theme.json");

    public static bool IsDark { get; private set; }
    public static string PrimaryColor { get; private set; } = "#0F6E56";

    /// <summary>Aplică tema salvată (sau Light dacă nu există preferință).</summary>
    public static void ApplySaved()
    {
        var saved = ReadPref();
        Apply(saved.Theme == "Dark");
        ApplyPrimaryColor(saved.Primary);
    }

    /// <summary>Comută între Light ↔ Dark și salvează preferința.</summary>
    public static void Toggle() => Apply(!IsDark);

    /// <summary>Aplică explicit o temă și salvează preferința.</summary>
    public static void Apply(bool dark)
    {
        IsDark = dark;
        var themeUri = new Uri($"pack://application:,,,/Mediclin.UI;component/{(dark ? DarkSource : LightSource)}");
        var merged = Application.Current.Resources.MergedDictionaries;

        try
        {
            // Înlocuim chirurgical primul dicționar (care trebuie să fie cel de culori, conform App.xaml)
            var newDict = new ResourceDictionary { Source = themeUri };
            
            if (merged.Count > 0)
            {
                merged[0] = newDict;
            }
            else
            {
                merged.Add(newDict);
            }
        }
        catch
        {
            // Fallback la metoda anterioară dacă indexul 0 nu este cel corect
            var existing = merged.FirstOrDefault(d =>
                d.Source != null &&
                (d.Source.ToString().Contains("Colors.xaml", StringComparison.OrdinalIgnoreCase) ||
                 d.Source.ToString().Contains("Colors.Dark.xaml", StringComparison.OrdinalIgnoreCase)));

            if (existing != null)
            {
                int idx = merged.IndexOf(existing);
                merged[idx] = new ResourceDictionary { Source = themeUri };
            }
        }

        SavePref(dark ? "Dark" : "Light");
    }

    public static void ApplyPrimaryColor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return;
        }

        try
        {
            var color = (Color)ColorConverter.ConvertFromString(hex)!;
            PrimaryColor = hex;
            var dark = Darken(color, 0.72);
            var light = IsDark ? Color.FromRgb(18, 62, 52) : Color.FromRgb(231, 244, 239);

            Application.Current.Resources["PrimaryGreen"] = color;
            Application.Current.Resources["PrimaryGreenDark"] = dark;
            Application.Current.Resources["PrimaryGreenLight"] = light;
            Application.Current.Resources["PrimaryGreenBrush"] = new SolidColorBrush(color);
            Application.Current.Resources["PrimaryGreenDarkBrush"] = new SolidColorBrush(dark);
            Application.Current.Resources["PrimaryGreenLightBrush"] = new SolidColorBrush(light);
            Application.Current.Resources["PrimaryLightBrush"] = new SolidColorBrush(light);
            SavePref(IsDark ? "Dark" : "Light");
        }
        catch
        {
            // culoare invalida - ignoram
        }
    }

    // ── Persistență ──────────────────────────────────────────────────────────

    private static (string Theme, string Primary) ReadPref()
    {
        try
        {
            if (!File.Exists(PrefFile)) return ("Light", PrimaryColor);
            var json = File.ReadAllText(PrefFile);
            var doc  = JsonDocument.Parse(json);
            var theme = doc.RootElement.TryGetProperty("theme", out var t) ? t.GetString() ?? "Light" : "Light";
            var primary = doc.RootElement.TryGetProperty("primary", out var p) ? p.GetString() ?? PrimaryColor : PrimaryColor;
            return (theme, primary);
        }
        catch { return ("Light", PrimaryColor); }
    }

    private static void SavePref(string value)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PrefFile)!);
            var json = JsonSerializer.Serialize(new { theme = value, primary = PrimaryColor });
            File.WriteAllText(PrefFile, json);
        }
        catch { /* ignorat */ }
    }

    private static Color Darken(Color color, double factor)
    {
        return Color.FromRgb(
            (byte)Math.Max(0, color.R * factor),
            (byte)Math.Max(0, color.G * factor),
            (byte)Math.Max(0, color.B * factor));
    }
}
