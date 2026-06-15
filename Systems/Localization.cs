using System.Globalization;
using System.Text.Json;
using MyriaLib.Systems.Enums;
using MyriaLib.Systems.Mods;

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
            file = ModLoader.ResolvePath(file);
            if (!File.Exists(file)) file = Path.Combine("Data", "locales", "en.json");
            file = Path.GetFullPath(file);

            try
            {
                var json = File.ReadAllText(file);
                _strings = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                           ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Failed to load localization file '{file}'.", ex);
            }

            ApplyModLocaleAdditions(lang);

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
            if (key == null)
                return "";
            if (!_strings.TryGetValue(key, out var format))
                return $"[{key}]"; // visible fallback so missing keys are easy to spot

            return args is { Length: > 0 }
                ? string.Format(Culture, format, args)
                : format;
        }

        private static void ApplyModLocaleAdditions(GameLanguage lang)
        {
            var localeKey = lang switch
            {
                GameLanguage.De => "de",
                _ => "en"
            };

            foreach (var mod in ModLoader.ActiveMods)
            {
                if (!mod.Manifest.LocaleAdditions.TryGetValue(localeKey, out var additions))
                    continue;

                foreach (var entry in additions)
                    _strings[entry.Key] = entry.Value;
            }
        }

    }

}
