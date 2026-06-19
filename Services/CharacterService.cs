using MyriaLib.Entities.Characters;
using MyriaLib.Models;
using MyriaLib.Services.Builder;
using MyriaLib.Systems;
using MyriaLib.Systems.Mods;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MyriaLib.Services
{
    public static class CharacterService
    {
        private static readonly JsonSerializerOptions _opts = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            Converters = { new ItemConverter(), new MoneyConverter() }
        };

        private static readonly JsonSerializerOptions _snapshotOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
        };

        private static string SavePath(UserAccount user, string characterName)
            => Path.Combine("Data/saves", $"{user.Username}-{characterName}.json");

        public static void DeleteCharacter(string name, UserAccount user)
        {
            string path = SavePath(user, name);
            if (File.Exists(path))
                File.Delete(path);
        }

        public static void SaveCharacter(UserAccount user, Character character)
        {
            string path = SavePath(user, character.Name);
            Directory.CreateDirectory("Data/saves");

            character.CurrentRoomId = character.CurrentRoom?.Id ?? character.CurrentRoomId;

            // Serialize character with its custom converters.
            string charJson = JsonSerializer.Serialize(character, _opts);

            // Wrap with the current mod snapshot so loading can detect drift.
            var snapshot = ModLoader.GetCurrentSnapshot();
            var wrapper  = new JsonObject
            {
                ["SavedWithMods"] = JsonNode.Parse(JsonSerializer.Serialize(snapshot, _snapshotOpts)),
                ["Character"]     = JsonNode.Parse(charJson),
            };

            File.WriteAllText(path, wrapper.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }

        /// <summary>
        /// Reads only the mod snapshot from a save file without loading the full character.
        /// Returns <c>null</c> if the file uses the old format (no snapshot stored).
        /// </summary>
        public static ModSnapshot? ReadModSnapshot(string name, UserAccount user)
        {
            string path = SavePath(user, name);
            if (!File.Exists(path)) return null;

            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                if (!doc.RootElement.TryGetProperty("SavedWithMods", out var el)) return null;
                return JsonSerializer.Deserialize<ModSnapshot>(el.GetRawText(), _snapshotOpts);
            }
            catch { return null; }
        }

        public static Character? LoadCharacter(string name, UserAccount user)
        {
            string path = SavePath(user, name);
            if (!File.Exists(path)) return null;

            var raw  = File.ReadAllText(path);
            string characterJson;

            // Support both new wrapper format ({ "SavedWithMods":…, "Character":… })
            // and the old format (bare Character JSON).
            try
            {
                using var doc = JsonDocument.Parse(raw);
                characterJson = doc.RootElement.TryGetProperty("Character", out var el)
                    ? el.GetRawText()
                    : raw;
            }
            catch { characterJson = raw; }

            var character = JsonSerializer.Deserialize<Character>(characterJson, _opts);
            if (character == null) return null;

            character.Inventory.Items.RemoveAll(i => i == null);

            // Migrate legacy saves that stored Class/Race as enum integers (e.g. "Class": 3).
            MigrateLegacyClassRace(character);

            try
            {
                character.CurrentRoom = RoomService.AllRooms.FirstOrDefault(r => r.Id == character.CurrentRoomId);
            }
            catch { /* room reconnect handled by caller */ }

            character.RecalculateUnusedPoints();
            character.ValidateQuestStatuses();
            SkillFactory.UpdateSkills(character);
            ResolveAdvancedSystems(character);
            return character;
        }

        public static List<Character> LoadCharacters(UserAccount account)
        {
            var characters = new List<Character>();
            foreach (var name in account.CharacterNames)
            {
                var c = LoadCharacter(name, account);
                if (c != null) characters.Add(c);
            }
            return characters;
        }

        /// <summary>
        /// Converts class/race values that were saved as enum integers (old format) to string IDs.
        /// Also converts numeric keys in ClassXp and Stashed* dictionaries.
        /// </summary>
        private static void MigrateLegacyClassRace(Character character)
        {
            // Class / Race — may be "3" (old int-serialised enum) instead of "Fighter"
            if (int.TryParse(character.Class, out var classInt)
                && MyriaLib.Systems.Enums.CharacterClass.FromLegacyInt.TryGetValue(classInt, out var classStr))
                character.Class = classStr;

            if (int.TryParse(character.Race, out var raceInt)
                && MyriaLib.Systems.Enums.CharacterRace.FromLegacyInt.TryGetValue(raceInt, out var raceStr))
                character.Race = raceStr;

            // ClassXp — keys may be "3" etc.
            var legacyXpKeys = character.ClassXp.Keys.Where(k => int.TryParse(k, out _)).ToList();
            foreach (var key in legacyXpKeys)
            {
                if (int.TryParse(key, out var n)
                    && MyriaLib.Systems.Enums.CharacterClass.FromLegacyInt.TryGetValue(n, out var newKey))
                {
                    character.ClassXp[newKey] = character.ClassXp[key];
                    character.ClassXp.Remove(key);
                }
            }

            // StashedCombinedSkills / StashedCompositeSkills
            MigrateDictKeys(character.StashedCombinedSkills);
            MigrateDictKeys(character.StashedCompositeSkills);
        }

        private static void MigrateDictKeys<T>(Dictionary<string, T> dict)
        {
            var legacyKeys = dict.Keys.Where(k => int.TryParse(k, out _)).ToList();
            foreach (var key in legacyKeys)
            {
                if (int.TryParse(key, out var n)
                    && MyriaLib.Systems.Enums.CharacterClass.FromLegacyInt.TryGetValue(n, out var newKey))
                {
                    dict[newKey] = dict[key];
                    dict.Remove(key);
                }
            }
        }

        private static void ResolveAdvancedSystems(Character character)
        {
            BaseRuneService.ResolveRunes(character);
            SkillFusionSystem.ResolveCompositeSkills(character);
            SkillCombinationService.ResolveCombinedSkills(character);
            SkillSlotService.ResolveSlots(character);
            SkillSlotService.MigrateIfEmpty(character);
        }
    }
}
