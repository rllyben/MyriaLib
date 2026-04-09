using MyriaLib.Entities.Players;
using MyriaLib.Models.BaseModel;
using MyriaLib.Systems;
using MyriaLib.Systems.Enums;
using System.Text.Json;

namespace MyriaLib.Services.Builder
{
    /// <summary>
    /// Loads and caches base skill component definitions used by the fusion system.
    /// These are the building blocks players combine to create composite skills.
    /// Call <see cref="Load"/> once at app start.
    /// </summary>
    public static class BaseSkillLoader
    {
        private static readonly string _path = "Data/common/base_skills.json";
        private static Dictionary<string, BaseSkillData> _skills = new(StringComparer.OrdinalIgnoreCase);

        public static void Load(string path = "")
        {
            string filePath = string.IsNullOrEmpty(path) ? _path : path;

            if (!File.Exists(filePath))
            {
                GameLog.Error($"Base skills file not found at '{filePath}'.");
                _skills = new(StringComparer.OrdinalIgnoreCase);
                return;
            }

            var list = JsonSerializer.Deserialize<List<BaseSkillData>>(
                           File.ReadAllText(filePath),
                           new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                       ?? new();

            _skills = list.ToDictionary(s => s.Id, s => s, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Returns a base skill definition by ID, or <c>null</c> if not found.</summary>
        public static BaseSkillData? Get(string id) =>
            _skills.TryGetValue(id, out var s) ? s : null;

        /// <summary>Returns all base skill definitions available to a specific player class.</summary>
        public static List<BaseSkillData> GetForClass(PlayerClass playerClass)
        {
            string className = playerClass.ToString();
            return _skills.Values
                .Where(s => s.Class.Equals(className, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }
}
