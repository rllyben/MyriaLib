using MyriaLib.Models;
using MyriaLib.Systems;
using System.Text.Json;

namespace MyriaLib.Services
{
    /// <summary>
    /// Loads and caches named fusion recipes. Used by <c>SkillFusionSystem</c> to override
    /// the algorithmically generated identity (and optionally stats) for specific combinations.
    /// Call <see cref="Load"/> once at app start.
    /// </summary>
    public static class FusionRecipeService
    {
        private static readonly string _path = "Data/common/fusion_recipes.json";

        // Key: sorted, joined component IDs (e.g. "slash:slash:slash")
        private static Dictionary<string, FusionRecipe> _recipes = new(StringComparer.OrdinalIgnoreCase);

        public static void Load(string path = "")
        {
            string filePath = string.IsNullOrEmpty(path) ? _path : path;

            if (!File.Exists(filePath))
            {
                // Not an error — recipes are optional enhancements on top of algorithmic fusion.
                _recipes = new(StringComparer.OrdinalIgnoreCase);
                return;
            }

            var list = JsonSerializer.Deserialize<List<FusionRecipe>>(
                           File.ReadAllText(filePath),
                           new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                       ?? new();

            _recipes = list.ToDictionary(
                r => MakeKey(r.ComponentIds),
                r => r,
                StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Finds a named recipe for the given component ID list, or returns <c>null</c> if none exists.
        /// Component order does not matter — matching is done on a sorted, normalized key.
        /// </summary>
        public static FusionRecipe? FindRecipe(List<string> componentIds) =>
            _recipes.TryGetValue(MakeKey(componentIds), out var recipe) ? recipe : null;

        private static string MakeKey(List<string> ids) =>
            string.Join(":", ids.Select(id => id.ToLowerInvariant()).OrderBy(id => id));
    }
}
