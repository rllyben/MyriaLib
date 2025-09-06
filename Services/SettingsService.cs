using System.Text.Json;
using MyriaLib.Systems;

namespace MyriaLib.Services
{
    public static class SettingsService
    {
        private static readonly string PathFile = System.IO.Path.Combine("Data", "Misc", "settings.json");

        public static GameSettings Current { get; private set; } = new();

        public static void Load()
        {
            try
            {
                if (!File.Exists(PathFile))
                {
                    Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PathFile)!);
                    Save(); // Write default
                    return;
                }

                var json = File.ReadAllText(PathFile);
                Current = JsonSerializer.Deserialize<GameSettings>(json) ?? new GameSettings();
            }
            catch
            {
                Current = new GameSettings();
                Save();
            }
            
        }

        public static void Save()
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PathFile)!);
            var json = JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(PathFile, json);
        }
    
    }

}