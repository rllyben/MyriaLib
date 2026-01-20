using MyriaLib.Models.Settings;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyriaLib.Services
{
    public static class SettingsService
    {
        private static readonly string PathFile = Path.Combine("Data", "Misc", "settings.json");

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            // Optional: write enums as strings
            Converters = { new JsonStringEnumConverter() }
        };
        public static void Load()
        {
            try
            {
                if (!File.Exists(PathFile))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(PathFile)!);
                    Save(); // write defaults
                    return;
                }

                var json = File.ReadAllText(PathFile);
                var loaded = JsonSerializer.Deserialize<Settings>(json, JsonOpts);
                Settings.Current = loaded ?? new Settings();

                // Optional: if file was empty -> persist defaults now
                if (loaded is null) Save();
            }
            catch
            {
                Settings.Current = new Settings();
                Save();
            }

        }


        public static void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PathFile)!);
            var json = JsonSerializer.Serialize(Settings.Current, JsonOpts);
            File.WriteAllText(PathFile, json);
        }

    }

}