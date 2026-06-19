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

        // ── Built-in language loading ─────────────────────────────────────────

        /// <summary>Loads a built-in language by enum value.</summary>
        public static void Load(GameLanguage lang)
        {
            var file = lang switch
            {
                GameLanguage.De => "Data/locales/de.json",
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

            var localeKey = lang == GameLanguage.De ? "de" : "en";
            ApplyModLocaleAdditions(localeKey);

            Culture = lang switch
            {
                GameLanguage.De => new CultureInfo("de-DE"),
                _ => new CultureInfo("en-US")
            };

            LanguageChanged?.Invoke(null, EventArgs.Empty);
        }

        // ── String-based loading (supports mod languages) ─────────────────────

        /// <summary>
        /// Loads a language by its string ID.
        /// Built-in IDs are "en" and "de". Any other ID is looked up in active mod language definitions.
        /// Falls back to English if the language is not found.
        /// </summary>
        public static void Load(string languageId)
        {
            if (string.IsNullOrWhiteSpace(languageId)) { Load(GameLanguage.En); return; }

            switch (languageId.ToLowerInvariant())
            {
                case "en": Load(GameLanguage.En); return;
                case "de": Load(GameLanguage.De); return;
            }

            // Look for a mod-provided language.
            var modLang = ModLoader.GetModLanguages()
                .FirstOrDefault(ml => string.Equals(ml.Def.Id, languageId, StringComparison.OrdinalIgnoreCase));

            if (modLang.Def != null)
                LoadModLanguage(modLang.Mod, modLang.Def);
            else
                Load(GameLanguage.En); // fallback
        }

        private static void LoadModLanguage(LoadedMod mod, ModLanguageDefinition def)
        {
            var file = Path.GetFullPath(Path.Combine(mod.Directory, def.LocaleFile));
            if (!File.Exists(file)) { Load(GameLanguage.En); return; }

            try
            {
                var json = File.ReadAllText(file);
                _strings = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                           ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            catch { Load(GameLanguage.En); return; }

            // Allow other mods to extend this language via LocaleAdditions.
            ApplyModLocaleAdditions(def.Id);

            Culture = string.IsNullOrWhiteSpace(def.CultureCode)
                ? CultureInfo.InvariantCulture
                : new CultureInfo(def.CultureCode);

            LanguageChanged?.Invoke(null, EventArgs.Empty);
        }

        // ── Translation ───────────────────────────────────────────────────────

        public static string T(string key, params object[] args)
        {
            if (key == null) return "";
            if (!_strings.TryGetValue(key, out var format))
                return $"[{key}]";

            return args is { Length: > 0 }
                ? string.Format(Culture, format, args)
                : format;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        // Locale additions are now provided by mods through their locale JSON files
        // (e.g. Data/locales/en.json), which are automatically merged by ModLoader.
        // No manifest-level locale additions remain.
        private static void ApplyModLocaleAdditions(string localeKey) { }
    }
}
