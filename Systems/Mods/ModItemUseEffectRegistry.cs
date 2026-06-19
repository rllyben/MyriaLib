using MyriaLib.Entities.Characters;
using MyriaLib.Entities.Items;
using MyriaLib.Services.Manager;

namespace MyriaLib.Systems.Mods
{
    public static class ModItemUseEffectRegistry
    {
        private sealed record ActiveEffect(string ModId, ModItemUseEffectDefinition Definition);

        private static readonly Dictionary<string, ActiveEffect> _byItemId = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, ActiveEffect> _byKey    = new(StringComparer.OrdinalIgnoreCase);

        public static void Load(ModLoadContext context)
        {
            _byItemId.Clear();
            _byKey.Clear();

            if (context.MultiplayerMode) return;

            foreach (var mod in context.GameplayMods)
            {
                foreach (var effect in mod.ItemEffects)
                {
                    if (string.IsNullOrWhiteSpace(effect.Key)) continue;

                    var active = new ActiveEffect(mod.Manifest.Id, effect);
                    _byKey[effect.Key] = active;
                    if (!string.IsNullOrWhiteSpace(effect.ItemId))
                        _byItemId[effect.ItemId] = active;
                }
            }
        }

        public static bool TryUse(ConsumableItem item, Character character)
        {
            if (!TryFindEffect(item, out var activeEffect)) return false;

            var effect = activeEffect.Definition;

            // ── Pre-conditions ────────────────────────────────────────────────
            if (!CheckCondition(effect.RequireCondition, character))
                return true; // item "consumed" but effect blocked — return true to suppress base effect

            if (!CheckCooldown(character, activeEffect.ModId, effect))
                return true; // on cooldown — consume item, do nothing

            // ── Daily-use tracking ────────────────────────────────────────────
            int dailyUseCount = 0;
            if (effect.KillOnDailyUseNumber > 0)
                dailyUseCount = IncrementDailyUseCount(character, activeEffect.ModId, effect);

            // ── Apply effects ─────────────────────────────────────────────────
            if (effect.ApplyBaseEffect)
                item.ApplyBaseEffect(character);

            if (effect.HealToFull)
                character.Heal(int.MaxValue, effect.Key);
            else if (effect.HealPercent > 0)
                character.Heal((int)(character.MaxHealth * effect.HealPercent), effect.Key);
            else if (effect.HealAmount > 0)
                character.Heal(effect.HealAmount, effect.Key);

            if (effect.RestoreManaToFull)
                character.RestoreMana(int.MaxValue, effect.Key);
            else if (effect.ManaPercent > 0)
                character.RestoreMana((int)(character.MaxMana * effect.ManaPercent), effect.Key);
            else if (effect.ManaAmount > 0)
                character.RestoreMana(effect.ManaAmount, effect.Key);

            // ── XP grants ─────────────────────────────────────────────────────
            foreach (var (id, amount) in effect.GrantXp)
            {
                if (amount <= 0) continue;
                // Try class XP first, fall back to job XP.
                if (Entities.Characters.ClassProfile.All.ContainsKey(id))
                    ClassManager.GrantClassXp(character, id, amount);
                else
                    JobManager.GrantSkillXp(character, id, amount);
            }

            // ── Overuse penalty ───────────────────────────────────────────────
            if (effect.KillOnDailyUseNumber > 0 && dailyUseCount >= effect.KillOnDailyUseNumber)
                character.ApplyDamage(int.MaxValue, GetDeathSource(effect));

            return true;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static bool TryFindEffect(ConsumableItem item, out ActiveEffect effect)
        {
            if (!string.IsNullOrWhiteSpace(item.UseEffect)
                && _byKey.TryGetValue(item.UseEffect, out effect!))
                return true;

            return _byItemId.TryGetValue(item.Id, out effect!);
        }

        private static bool CheckCondition(string condition, Character character)
        {
            if (string.IsNullOrWhiteSpace(condition)) return true;
            return condition.Trim().ToLowerInvariant() switch
            {
                "hp_below_half" => character.CurrentHealth < character.MaxHealth / 2,
                "hp_full"       => character.CurrentHealth >= character.MaxHealth,
                "mana_empty"    => character.CurrentMana <= 0,
                "not_in_combat" => true,  // can always use outside combat context here
                _               => true   // unknown condition — allow by default
            };
        }

        private static bool CheckCooldown(Character character, string modId, ModItemUseEffectDefinition effect)
        {
            if (effect.CooldownMinutes <= 0) return true;

            var key = !string.IsNullOrWhiteSpace(effect.CooldownKey)
                ? $"cooldown.{effect.CooldownKey}"
                : $"cooldown.{modId}.{effect.Key}";

            if (character.ModState.TryGetValue(key, out var raw)
                && DateTime.TryParse(raw, null, System.Globalization.DateTimeStyles.RoundtripKind, out var last)
                && (DateTime.UtcNow - last).TotalMinutes < effect.CooldownMinutes)
                return false;

            character.ModState[key] = DateTime.UtcNow.ToString("O");
            return true;
        }

        private static int IncrementDailyUseCount(Character character, string modId, ModItemUseEffectDefinition effect)
        {
            var key     = GetDailyCounterKey(modId, effect);
            var dayKey  = key + ".day";
            var cntKey  = key + ".count";
            var today   = DayCycleManager.GameDay;

            var prevDay = TryGetInt(character.ModState, dayKey);
            var count   = prevDay == today ? TryGetInt(character.ModState, cntKey) : 0;

            count++;
            character.ModState[dayKey] = today.ToString();
            character.ModState[cntKey] = count.ToString();
            return count;
        }

        private static string GetDailyCounterKey(string modId, ModItemUseEffectDefinition effect)
            => string.IsNullOrWhiteSpace(effect.DailyCounterKey)
                ? $"mod.{modId}.{effect.Key}"
                : effect.DailyCounterKey;

        private static string GetDeathSource(ModItemUseEffectDefinition effect)
            => string.IsNullOrWhiteSpace(effect.DeathSource) ? effect.Key : effect.DeathSource;

        private static int TryGetInt(Dictionary<string, string> d, string key)
            => d.TryGetValue(key, out var raw) && int.TryParse(raw, out var v) ? v : 0;
    }
}
