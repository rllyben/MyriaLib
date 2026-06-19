using MyriaLib.Entities.Characters;
using MyriaLib.Entities.Skills;
using MyriaLib.Models;
using MyriaLib.Systems;
using MyriaLib.Systems.Enums;
using MyriaLib.Utils;
using System.Text.Json;

namespace MyriaLib.Services.Builder
{
    public static class SkillFactory
    {
        private static List<Skill> _skills = new();
        private static Dictionary<string, Skill> _skillsById = new();
        // private static List<BaseSkill> _baseSkills = new();

        public static void LoadSkills(string path = "Data/common/skills.json")
        {
            if (!File.Exists(path))
            {
                GameLog.Error($"Skills file not found at '{path}' — no skills loaded.");
                _skills = new();
                _skillsById = new();
                return;
            }

            var skillJson = File.ReadAllText(path);
            var skillData = JsonSerializer.Deserialize<List<SkillData>>(skillJson);
            if (skillData == null)
            {
                GameLog.Error($"Failed to deserialize skills from '{path}'.");
                _skills = new();
                _skillsById = new();
                return;
            }

            _skills = skillData.Select(d => new Skill
            {
                Id              = d.Id,
                Name            = d.Name,
                Description     = d.Description,
                Class           = d.Class,
                ManaCost        = d.ManaCost,
                Type            = Enum.Parse<SkillType>(d.Type),
                Target          = Enum.Parse<SkillTarget>(d.Target),
                ScalingFactor   = d.ScalingFactor,
                StatToScaleFrom = d.StatToScaleFrom,
                MinLevel        = d.MinLevel,
                IsHealing       = d.IsHealing
            }).ToList();

            _skillsById = _skills.ToDictionary(s => s.Id);
        }

        /// <summary>Returns a skill by ID, or <c>null</c> if not found.</summary>
        public static Skill? GetSkill(string id) =>
            _skillsById.TryGetValue(id, out var skill) ? skill : null;

        public static List<Skill> GetSkillsFor(Character character)
        {
            return _skills.Where(s => s.Class.Equals(character.Class, StringComparison.OrdinalIgnoreCase) && s.MinLevel <= character.Level).ToList();
        }
        //public static List<BaseSkill> GetBaseSkillsFor(Character character)
        //{
        //    return _baseSkills
        //        .Where(bs => bs.Class == character.Class && bs.RequiredLevel <= character.Level)
        //        .ToList();
        //}
        public static void UpdateSkills(Character character)
        {
            var unlocked = GetSkillsFor(character);
            foreach (var skill in unlocked)
            {
                character.LearnSkill(skill);
            }

            if (character.Class.Equals(CharacterClass.RunicMage, StringComparison.OrdinalIgnoreCase))
                BaseRuneService.GrantBaseRunes(character);
        }

    }

}
