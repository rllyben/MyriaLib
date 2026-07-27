using MyriaLib.Entities.Effects;
using MyriaLib.Systems;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyriaLib.Services.Builder
{
    public static class EffectFactory
    {
        private static Dictionary<string, EffectDefinition> _definitions =
            new(StringComparer.OrdinalIgnoreCase);

        public static void LoadEffects(string path = "Data/common/effects.json")
        {
            if (!File.Exists(path))
            {
                GameLog.Error($"Effects file not found at '{path}' — no effects loaded.");
                _definitions = new(StringComparer.OrdinalIgnoreCase);
                return;
            }

            var json = File.ReadAllText(path);
            var jsonOptions = new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter() }
            };
            var list = JsonSerializer.Deserialize<List<EffectDefinition>>(json, jsonOptions);
            if (list == null)
            {
                GameLog.Error($"Failed to deserialize effects from '{path}'.");
                _definitions = new(StringComparer.OrdinalIgnoreCase);
                return;
            }

            LoadEffects(list);
        }

        /// <summary>Loads effect definitions from already-parsed data (e.g. read from a database).</summary>
        public static void LoadEffects(List<EffectDefinition> list)
        {
            _definitions = list.ToDictionary(d => d.Id, d => d, StringComparer.OrdinalIgnoreCase);
        }

        public static EffectDefinition? GetDefinition(string id)
            => _definitions.TryGetValue(id, out var def) ? def : null;

        /// <param name="effectStat">
        /// The caster's primary stat value for this skill (e.g. TotalSTR, TotalSPR).
        /// Defaults to 10 so callers that omit it get the same result as the old flat formula.
        /// LifeSteal effects always use the flat formula regardless of this value.
        /// </param>
        public static ActiveEffect? CreateEffect(string id, string sourceSkillId = "", float scalingFactor = 1f, int effectStat = 10)
        {
            var def = GetDefinition(id);
            if (def == null) return null;

            // LifeSteal magnitude is a fixed fraction of damage — never stat-scale it.
            float magnitude = def.Type == EffectType.LifeSteal
                ? def.Magnitude * scalingFactor
                : def.Magnitude * scalingFactor * effectStat / 10f;

            return new ActiveEffect
            {
                DefinitionId   = def.Id,
                Name           = def.Name,
                Type           = def.Type,
                TurnsRemaining = def.Duration,
                Magnitude      = magnitude,
                StatName       = def.StatName,
                SourceSkillId  = sourceSkillId,
            };
        }
    }
}
