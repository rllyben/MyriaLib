using System.Globalization;
using System.Text.Json;
using MyriaLib.Systems.Enums;

namespace MyriaLib.Systems
{
    public static class Localization
    {
        private static Dictionary<string, string> _strings = new(StringComparer.OrdinalIgnoreCase);
        public static CultureInfo Culture { get; private set; } = CultureInfo.InvariantCulture;

        public static event EventHandler? LanguageChanged;
        public static void Load(GameLanguage lang)
        {
            var file = lang switch
            {
                GameLanguage.De => "Data/locales/de.json",
                //GameLanguage.Fr => "Data/locales/fr.json",
                //GameLanguage.Es => "Data/locales/es.json",
                _ => "Data/locales/en.json"
            };
            if (!File.Exists(file)) file = "Data/locales/en.json";

            var json = File.ReadAllText(file);
            _strings = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                       ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            Culture = lang switch
            {
                GameLanguage.De => new CultureInfo("de-DE"),
                //GameLanguage.Fr => new CultureInfo("fr-FR"),
                //GameLanguage.Es => new CultureInfo("es-ES"),
                _ => new CultureInfo("en-US")
            };

            LanguageChanged?.Invoke(null, EventArgs.Empty);
        }

        public static string T(string key, params object[] args)
        {
            if (!_strings.TryGetValue(key, out var format))
                return $"[{key}]"; // visible fallback so missing keys are easy to spot

            return args is { Length: > 0 }
                ? string.Format(Culture, format, args)
                : format;
        }

    }

}
