using MyriaLib.Entities.Players;
using MyriaLib.Entities.Skills;
using MyriaLib.Models;
using MyriaLib.Systems.Enums;
using MyriaLib.Utils;
using System.Text.Json;

namespace MyriaLib.Services.Builder
{
    public static class SkillFactory
    {
        private static List<Skill> _skills = new();
        // private static List<BaseSkill> _baseSkills = new();

        public static void LoadSkills(string path = "Data/common/skills.json")
        {
            var skillJson = File.ReadAllText(path);
            var skillData = JsonSerializer.Deserialize<List<SkillData>>(skillJson);

            _skills = skillData.Select(d => new Skill
            {
                Id = d.Id,
                Name = d.Name,
                Description = d.Description,
                Class = Enum.Parse<PlayerClass>(d.Class),
                ManaCost = d.ManaCost,
                Type = Enum.Parse<SkillType>(d.Type),
                Target = Enum.Parse<SkillTarget>(d.Target),
                ScalingFactor = d.ScalingFactor,
                StatToScaleFrom = d.StatToScaleFrom,
                MinLevel = d.MinLevel,
                IsHealing = d.IsHealing
            }).ToList();

        }

        public static List<Skill> GetSkillsFor(Player player)
        {
            return _skills.Where(s => s.Class == player.Class && s.MinLevel <= player.Level).ToList();
        }
        //public static List<BaseSkill> GetBaseSkillsFor(Player player)
        //{
        //    return _baseSkills
        //        .Where(bs => bs.Class == player.Class && bs.RequiredLevel <= player.Level)
        //        .ToList();
        //}
        public static void UpdateSkills(Player player)
        {
            var unlocked = GetSkillsFor(player);
            foreach (var skill in unlocked)
            {
                player.LearnSkill(skill);
            }

        }

    }

}
