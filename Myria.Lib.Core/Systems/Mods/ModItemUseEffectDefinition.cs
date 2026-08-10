namespace Myria.Lib.Core.Systems.Mods
{
    /// <summary>
    /// Defines a custom use-effect for a consumable item, declared in a mod's
    /// <c>item_effects.json</c>. No C# knowledge needed — all fields are data-driven.
    /// </summary>
    public sealed class ModItemUseEffectDefinition
    {
        // ── Identity ─────────────────────────────────────────────────────────

        /// <summary>Unique key used to reference this effect from an item's <c>"useEffect"</c> field.</summary>
        public string Key { get; set; } = "";

        /// <summary>Alternatively register the effect directly on an item by its ID.</summary>
        public string ItemId { get; set; } = "";

        // ── Condition ─────────────────────────────────────────────────────────

        /// <summary>
        /// Optional pre-condition. If the condition is not met, the item use is rejected.
        /// Supported values: <c>"hp_below_half"</c>, <c>"hp_full"</c>, <c>"mana_empty"</c>,
        /// <c>"not_in_combat"</c>.
        /// </summary>
        public string RequireCondition { get; set; } = "";

        // ── Cooldown ──────────────────────────────────────────────────────────

        /// <summary>Real-time cooldown in minutes between successive uses. 0 = no cooldown.</summary>
        public int CooldownMinutes { get; set; }

        /// <summary>
        /// Key used to identify the cooldown bucket in <see cref="Myria.Lib.Core.Entities.Characters.Character.ModState"/>.
        /// Defaults to <c>mod.{modId}.{Key}</c> when empty.
        /// </summary>
        public string CooldownKey { get; set; } = "";

        // ── Healing ───────────────────────────────────────────────────────────

        /// <summary>Whether the item's built-in heal/mana restore also applies.</summary>
        public bool ApplyBaseEffect { get; set; } = true;

        /// <summary>Heal the character by this fixed amount.</summary>
        public int HealAmount { get; set; }

        /// <summary>Heal the character by this fraction of their max HP (0.0–1.0).</summary>
        public float HealPercent { get; set; }

        /// <summary>Restore mana by this fixed amount.</summary>
        public int ManaAmount { get; set; }

        /// <summary>Restore mana by this fraction of max mana (0.0–1.0).</summary>
        public float ManaPercent { get; set; }

        /// <summary>Heal the character to full HP.</summary>
        public bool HealToFull { get; set; }

        /// <summary>Restore the character to full mana.</summary>
        public bool RestoreManaToFull { get; set; }

        // ── XP grants ────────────────────────────────────────────────────────

        /// <summary>
        /// Award XP after use. Keys are class IDs or job IDs; values are XP amounts.
        /// Example: <c>{ "Rogue": 50, "hunter": 25 }</c>
        /// </summary>
        public Dictionary<string, int> GrantXp { get; set; } = new();

        // ── Overuse penalty ───────────────────────────────────────────────────

        /// <summary>Kill the character on the Nth use of this item per in-game day. 0 = disabled.</summary>
        public int KillOnDailyUseNumber { get; set; }

        /// <summary>Key used to count daily uses. Defaults to a per-mod, per-key value.</summary>
        public string DailyCounterKey { get; set; } = "";

        /// <summary>Name shown in the death log when killed by this effect.</summary>
        public string DeathSource { get; set; } = "";
    }
}
