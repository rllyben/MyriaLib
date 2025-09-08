using MyriaLib.Systems.Enums;

namespace MyriaLib.Models.Settings
{
    public class Settings
    {
        public static Settings Current { get; set; } = new Settings();
        public Language LanguageSettings { get; set; } = new();
        public Visuals VisualSettings { get; set; } = new();

    }

}
