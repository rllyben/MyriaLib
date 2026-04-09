using MyriaLib.Entities.Players;
using MyriaLib.Models;
using MyriaLib.Services.Builder;
using MyriaLib.Systems;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyriaLib.Services
{
    public static class CharacterService
    {
        public static void SaveCharacter(UserAccount user, Player player)
        {
            string path = Path.Combine("Data/saves", $"{user.Username}-{player.Name}.json");

            if (!Path.Exists(path))
                Directory.CreateDirectory("Data/saves");

            player.CurrentRoomId = player.CurrentRoom.Id;

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
                Converters = { new ItemConverter() }
            };
            var json = JsonSerializer.Serialize(player, options);
            File.WriteAllText(path, json);
        }

        public static Player LoadCharacter(string name, UserAccount user)
        {
            string path = Path.Combine("Data/saves", $"{user.Username}-{name}.json");
            var json = File.ReadAllText(path);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new ItemConverter() }
            };

            var jsonHero = JsonSerializer.Deserialize<Player>(json, options);
            Player player = jsonHero;
            try
            {
                int roomId = player.CurrentRoomId;
                player.CurrentRoom = RoomService.AllRooms.FirstOrDefault(r => r.Id == roomId);
            }
            catch (Exception ex)
            {
                // Log error or handle gracefully
            }
            
            // Recalculate unused points for imported/loaded characters
            player.RecalculateUnusedPoints();
            player.ValidateQuestStatuses();

            SkillFactory.UpdateSkills(player);
            ResolveAdvancedSystems(player);
            return player;
        }
        public static List<Player> LoadCharacters(UserAccount account)
        {
            List<Player> characters = new List<Player>();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new ItemConverter() }
            };

            foreach (string characterName in account.CharacterNames)
            {
                string path = Path.Combine("Data/saves", $"{account.Username}-{characterName}.json");
                var json = File.ReadAllText(path);

                var jsonHero = JsonSerializer.Deserialize<Player>(json, options);
                Player player = jsonHero;
                try
                {
                    int roomId = player.CurrentRoomId;
                    player.CurrentRoom = RoomService.AllRooms.FirstOrDefault(r => r.Id == roomId);
                }
                catch (Exception ex) { }

                player.RecalculateUnusedPoints();
                player.ValidateQuestStatuses();

                SkillFactory.UpdateSkills(player);
                ResolveAdvancedSystems(player);
                characters.Add(player);
            }
            return characters;
        }

        /// <summary>
        /// Re-resolves fusion and runic skill data after a player is loaded from save.
        /// Safe to call for console players and old saves — empty collections are a no-op.
        /// </summary>
        private static void ResolveAdvancedSystems(Player player)
        {
            BaseRuneService.ResolveRunes(player);
            SkillFusionSystem.ResolveCompositeSkills(player);
        }

    }

}