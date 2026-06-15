using MyriaLib.Entities.Characters;
using MyriaLib.Entities.Items;
using MyriaLib.Services.Manager;

namespace MyriaLib.Systems.Mods
{
    public static class ModItemUseEffectRegistry
    {
        private sealed record ActiveEffect(string ModId, ModItemUseEffectDefinition Definition);

        private static readonly Dictionary<string, ActiveEffect> _byItemId = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, ActiveEffect> _byKey = new(StringComparer.OrdinalIgnoreCase);

        public static void Load(ModLoadContext context)
        {
            _byItemId.Clear();
            _byKey.Clear();

            if (context.MultiplayerMode)
                return;

            foreach (var mod in context.GameplayMods)
            {
                foreach (var effect in mod.Manifest.ItemUseEffects)
                {
                    if (string.IsNullOrWhiteSpace(effect.Key))
                        continue;

                    var activeEffect = new ActiveEffect(mod.Manifest.Id, effect);
                    _byKey[effect.Key] = activeEffect;

                    if (!string.IsNullOrWhiteSpace(effect.ItemId))
                        _byItemId[effect.ItemId] = activeEffect;
                }
            }
        }

        public static bool TryUse(ConsumableItem item, Character character)
        {
            if (!TryFindEffect(item, out var activeEffect))
                return false;

            var effect = activeEffect.Definition;
            var dailyUseCount = 0;

            if (effect.KillOnDailyUseNumber > 0)
                dailyUseCount = IncrementDailyUseCount(character, activeEffect.ModId, effect);

            if (effect.ApplyBaseEffect)
                item.ApplyBaseEffect(character);

            if (effect.HealToFull)
                character.Heal(int.MaxValue, effect.Key);

            if (effect.RestoreManaToFull)
                character.RestoreMana(int.MaxValue, effect.Key);

            if (effect.KillOnDailyUseNumber > 0 && dailyUseCount >= effect.KillOnDailyUseNumber)
                character.ApplyDamage(int.MaxValue, GetDeathSource(effect));

            return true;
        }

        private static bool TryFindEffect(ConsumableItem item, out ActiveEffect effect)
        {
            if (!string.IsNullOrWhiteSpace(item.UseEffect)
                && _byKey.TryGetValue(item.UseEffect, out effect!))
            {
                return true;
            }

            return _byItemId.TryGetValue(item.Id, out effect!);
        }

        private static int IncrementDailyUseCount(
            Character character,
            string modId,
            ModItemUseEffectDefinition effect)
        {
            var key = GetDailyCounterKey(modId, effect);
            var dayKey = key + ".day";
            var countKey = key + ".count";
            var today = DayCycleManager.GameDay;

            var previousDay = TryGetInt(character.ModState, dayKey);
            var count = previousDay == today
                ? TryGetInt(character.ModState, countKey)
                : 0;

            count++;
            character.ModState[dayKey] = today.ToString();
            character.ModState[countKey] = count.ToString();
            return count;
        }

        private static string GetDailyCounterKey(string modId, ModItemUseEffectDefinition effect)
        {
            if (!string.IsNullOrWhiteSpace(effect.DailyCounterKey))
                return effect.DailyCounterKey;

            return $"mod.{modId}.{effect.Key}";
        }

        private static string GetDeathSource(ModItemUseEffectDefinition effect)
        {
            return string.IsNullOrWhiteSpace(effect.DeathSource)
                ? effect.Key
                : effect.DeathSource;
        }

        private static int TryGetInt(Dictionary<string, string> values, string key)
        {
            return values.TryGetValue(key, out var raw) && int.TryParse(raw, out var value)
                ? value
                : 0;
        }
    }
}
