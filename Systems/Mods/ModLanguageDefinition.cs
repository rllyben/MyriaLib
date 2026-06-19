namespace MyriaLib.Systems.Mods
{
    /// <summary>
    /// Declares a new language provided by a mod.
    /// The locale file is loaded from the mod's directory at <see cref="LocaleFile"/>.
    /// </summary>
    public class ModLanguageDefinition
    {
        /// <summary>Short identifier used in settings and for <c>LocaleAdditions</c> lookups, e.g. "fr", "es", "zh-hans".</summary>
        public string Id { get; set; } = "";

        /// <summary>Human-readable name shown in the language selector, e.g. "Français".</summary>
        public string DisplayName { get; set; } = "";

        /// <summary>BCP-47 culture code for number/date formatting, e.g. "fr-FR". Leave empty to use InvariantCulture.</summary>
        public string CultureCode { get; set; } = "";

        /// <summary>Path to the locale JSON file, relative to the mod's folder, e.g. "Data/locales/fr.json".</summary>
        public string LocaleFile { get; set; } = "";
    }
}
