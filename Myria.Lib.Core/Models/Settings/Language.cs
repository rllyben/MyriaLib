using Myria.Lib.Core.Systems.Enums;

namespace Myria.Lib.Core.Models.Settings
{
    public class Language
    {
        /// <summary>The selected built-in language. Ignored when <see cref="ModLanguageId"/> is set.</summary>
        public GameLanguage Local { get; set; }

        /// <summary>
        /// When non-null, overrides <see cref="Local"/> with a mod-provided language ID (e.g. "fr", "es").
        /// Set to null to use a built-in language.
        /// </summary>
        public string? ModLanguageId { get; set; }

        /// <summary>The effective string language ID to pass to <c>Localization.Load(string)</c>.</summary>
        public string EffectiveLanguageId =>
            string.IsNullOrWhiteSpace(ModLanguageId)
                ? (Local == GameLanguage.De ? "de" : "en")
                : ModLanguageId;
    }
}
