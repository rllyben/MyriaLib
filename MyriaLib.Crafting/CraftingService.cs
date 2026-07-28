using System.Text.Json;

namespace MyriaLib.Services
{
    public record RecipeIngredient(string ItemId, int Amount);

    /// <param name="RequiredKnowledgeLevel">Minimum knowledge level for the NPC's job needed to unlock this recipe.</param>
    public record CraftingRecipe(string OutputId, long XpReward, int RequiredKnowledgeLevel, RecipeIngredient[] Ingredients);

    /// <summary>
    /// Authoritative recipe registry. Single source of truth used by both
    /// the server (validation) and the WPF client (UI display).
    /// Recipes are loaded from <c>Data/common/recipes.json</c> at startup.
    /// A missing <c>RequiredKnowledgeLevel</c> in the JSON defaults to 1 (always unlocked).
    /// </summary>
    public static class CraftingService
    {
        private static Dictionary<string, CraftingRecipe[]> _byNpc = new(StringComparer.OrdinalIgnoreCase);

        public static void LoadRecipes(string path = "Data/common/recipes.json")
        {
            if (!File.Exists(path)) return;
            var raw = JsonSerializer.Deserialize<Dictionary<string, RecipeDto[]>>(File.ReadAllText(path)) ?? new();
            var recipes = new Dictionary<string, CraftingRecipe[]>(StringComparer.OrdinalIgnoreCase);
            foreach (var (npcId, dtos) in raw)
                recipes[npcId] = dtos.Select(d => new CraftingRecipe(
                    d.OutputId,
                    d.XpReward,
                    d.RequiredKnowledgeLevel,
                    d.Ingredients.Select(i => new RecipeIngredient(i.ItemId, i.Amount)).ToArray()
                )).ToArray();
            LoadRecipes(recipes);
        }

        /// <summary>Loads recipes from already-parsed data (e.g. read from a database).</summary>
        public static void LoadRecipes(Dictionary<string, CraftingRecipe[]> recipesByNpc)
        {
            _byNpc = new(recipesByNpc, StringComparer.OrdinalIgnoreCase);
        }

        public static CraftingRecipe? GetRecipe(string npcId, string outputId)
        {
            if (!_byNpc.TryGetValue(npcId, out var recipes)) return null;
            return recipes.FirstOrDefault(r => r.OutputId == outputId);
        }

        public static CraftingRecipe[] GetRecipes(string npcId)
        {
            _byNpc.TryGetValue(npcId, out var recipes);
            return recipes ?? [];
        }

        /// <summary>Material required to upgrade equipment at this NPC's category.</summary>
        public static string UpgradeMaterialFor(string? upgradeCategory) => upgradeCategory switch
        {
            "leathersmith" => "cured_leather",
            "tailor"       => "bolt_of_cloth",
            "artificer"    => "earth_essence",
            _              => "iron_ingot"
        };

        /// <summary>How many upgrade materials are needed based on current item level.</summary>
        public static int UpgradeMaterialCount(int upgradeLevel) =>
            upgradeLevel < 4 ? 1 : upgradeLevel < 7 ? 2 : 3;

        // ── JSON DTOs ─────────────────────────────────────────────────────────

        private class RecipeDto
        {
            public string OutputId { get; set; } = "";
            public long XpReward { get; set; }
            public int RequiredKnowledgeLevel { get; set; } = 1;
            public IngredientDto[] Ingredients { get; set; } = [];
        }

        private class IngredientDto
        {
            public string ItemId { get; set; } = "";
            public int Amount { get; set; }
        }
    }
}
