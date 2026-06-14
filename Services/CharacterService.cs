using MyriaLib.Entities.Characters;
using MyriaLib.Models;
using MyriaLib.Services.Builder;
using MyriaLib.Systems;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyriaLib.Services
{
    public static class CharacterService
    {
        public static void DeleteCharacter(string name, UserAccount user)
        {
            string path = Path.Combine("Data/saves", $"{user.Username}-{name}.json");
            if (File.Exists(path))
                File.Delete(path);
        }

        public static void SaveCharacter(UserAccount user, Character character)
        {
            string path = Path.Combine("Data/saves", $"{user.Username}-{character.Name}.json");

            if (!Path.Exists(path))
                Directory.CreateDirectory("Data/saves");

            character.CurrentRoomId = character.CurrentRoom.Id;

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
                Converters = { new ItemConverter(), new MoneyConverter() }
            };
            var json = JsonSerializer.Serialize(character, options);
            File.WriteAllText(path, json);
        }

        public static Character? LoadCharacter(string name, UserAccount user)
        {
            string path = Path.Combine("Data/saves", $"{user.Username}-{name}.json");
            if (!File.Exists(path))
                return null;
            var json = File.ReadAllText(path);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new ItemConverter(), new MoneyConverter() }
            };

            var jsonHero = JsonSerializer.Deserialize<Character>(json, options);
            Character character = jsonHero;
            character.Inventory.Items.RemoveAll(i => i == null);
            try
            {
                int roomId = character.CurrentRoomId;
                character.CurrentRoom = RoomService.AllRooms.FirstOrDefault(r => r.Id == roomId);
            }
            catch (Exception ex)
            {
                // Log error or handle gracefully
            }

            // Recalculate unused points for imported/loaded characters
            character.RecalculateUnusedPoints();
            character.ValidateQuestStatuses();

            SkillFactory.UpdateSkills(character);
            ResolveAdvancedSystems(character);
            return character;
        }
        public static List<Character> LoadCharacters(UserAccount account)
        {
            List<Character> characters = new List<Character>();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new ItemConverter(), new MoneyConverter() }
            };

            foreach (string characterName in account.CharacterNames)
            {
                string path = Path.Combine("Data/saves", $"{account.Username}-{characterName}.json");
                var json = File.ReadAllText(path);

                var jsonHero = JsonSerializer.Deserialize<Character>(json, options);
                Character character = jsonHero;
                character.Inventory.Items.RemoveAll(i => i == null);
                try
                {
                    int roomId = character.CurrentRoomId;
                    character.CurrentRoom = RoomService.AllRooms.FirstOrDefault(r => r.Id == roomId);
                }
                catch (Exception ex) { }

                character.RecalculateUnusedPoints();
                character.ValidateQuestStatuses();

                SkillFactory.UpdateSkills(character);
                ResolveAdvancedSystems(character);
                characters.Add(character);
            }
            return characters;
        }

        /// <summary>
        /// Re-resolves fusion and runic skill data after a character is loaded from save.
        /// Safe to call for console characters and old saves — empty collections are a no-op.
        /// </summary>
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