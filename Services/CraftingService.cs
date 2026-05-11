namespace MyriaLib.Services
{
    public record RecipeIngredient(string ItemId, int Amount);

    /// <param name="RequiredKnowledgeLevel">Minimum knowledge level for the NPC's job needed to unlock this recipe.</param>
    public record CraftingRecipe(string OutputId, long XpReward, int RequiredKnowledgeLevel, RecipeIngredient[] Ingredients);

    /// <summary>
    /// Authoritative recipe registry. Single source of truth used by both
    /// the server (validation) and the WPF client (UI display).
    /// </summary>
    public static class CraftingService
    {
        private static readonly Dictionary<string, CraftingRecipe[]> _byNpc =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["smith_default"] =
                [
                    new("iron_ingot",   5,  1,  [new("iron_ore",   2)]),
                    new("iron_sword",   30, 5,  [new("iron_ingot", 3)]),
                    new("chain_armor",  50, 10, [new("iron_ingot", 5)]),
                ],
                ["leathersmith_default"] =
                [
                    new("cured_leather", 3,  1, [new("feral_leather", 2)]),
                    new("leather_vest",  20, 5, [new("cured_leather", 4)]),
                ],
                ["tailor_default"] =
                [
                    new("linen_robe", 20, 5, [new("bolt_of_cloth", 4)]),
                ],
                ["artificer_default"] =
                [
                    new("imbued_ring", 30, 10, [new("iron_ingot", 2), new("earth_essence", 2)]),
                ],
            };

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
    }
}
