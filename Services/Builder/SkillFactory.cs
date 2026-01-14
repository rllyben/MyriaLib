using System.Text.Json;
using MyriaLib.Entities.Skills;
using MyriaLib.Entities.Players;
using MyriaLib.Models;
using MyriaLib.Systems.Enums;

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

            //var baseJson = File.ReadAllText("Data/baseSkills.json");
            //var baseData = JsonSerializer.Deserialize<List<BaseSkillData>>(baseJson);

            //_baseSkills = baseData.Select(d => new BaseSkill
            //{
            //    Id = d.Id,
            //    Name = d.Name,
            //    Description = d.Description,
            //    Class = Enum.Parse<PlayerClass>(d.Class),
            //    ManaCost = d.ManaCost,
            //    ScalingFactor = d.ScalingFactor,
            //    StatToScaleFrom = d.StatToScaleFrom,
            //    RequiredLevel = d.RequiredLevel,
            //}).ToList();

            //foreach (var skill in baseData)
            //{
            //    BaseSkill baseSkill = _baseSkills.FirstOrDefault(bs => bs.Id == skill.Id);
            //    foreach (string componentType in skill.ComponentType)
            //    {
            //        baseSkill.ComponentType.Add(Enum.Parse<SkillComponentType>(componentType));
            //    }

            //}

        }

        public static List<Skill> GetSkillsFor(Player player)
        {
            return _skills
                .Where(s => s.Class == player.Class && s.MinLevel <= player.Level)
                .ToList();
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
                if (!player.Skills.Any(s => s.Id == skill.Id))
                {
                    player.Skills.Add(skill);
                }

            }

        }

    }

}
