using Myria.Lib.Core.Systems.Enums;

namespace Myria.Lib.Core.Models.Settings
{
    public class Settings
    {
        public static Settings Current { get; set; } = new Settings();
        public Language LanguageSettings { get; set; } = new();
        public Visuals VisualSettings { get; set; } = new();
        public KeybindingSettings Keybindings { get; set; } = new();

        /// <summary>Auth/lobby server address (e.g. "http://myria.duckdns.org:5000"), typed in on
        /// the Game settings tab. Empty means "use the built-in localhost default" for local dev.</summary>
        public string ServerAddress { get; set; } = "";

        /// <summary>Whether to check for and silently install alpha updates on startup.</summary>
        public bool AutoUpdateEnabled { get; set; } = true;

    }

}
