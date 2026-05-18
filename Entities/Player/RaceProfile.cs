using MyriaLib.Systems.Enums;

namespace MyriaLib.Entities.Players
{
    public class RaceProfile
    {
        public PlayerRace Race { get; set; }
        public Dictionary<string, int> BaseStatBonus { get; set; } = new();
        public int BaseHpBonus { get; set; }
        public int BaseManaBonus { get; set; }
        public Dictionary<string, int> StatGrowth { get; set; } = new();
        public int HpPerLevel { get; set; }
        public int ManaPerLevel { get; set; }
        public HashSet<PlayerClass> ForbiddenClasses { get; set; } = new();

        private static readonly HashSet<PlayerClass> _nonAmatoForbidden = new()
        {
            PlayerClass.RunicMage
        };

        private static readonly HashSet<PlayerClass> _amatoForbidden = new()
        {
            PlayerClass.ElementalMage,
            PlayerClass.ArcanMage,
            PlayerClass.Druid,
            PlayerClass.SoulsKnight
        };

        public static Dictionary<PlayerRace, RaceProfile> All => new()
        {
            [PlayerRace.Myralu] = new RaceProfile
            {
                Race = PlayerRace.Myralu,
                BaseStatBonus = new() { ["STR"] = 3, ["DEX"] = 1, ["END"] = 7, ["INT"] = 3, ["SPR"] = 3 },
                BaseHpBonus = 30,
                BaseManaBonus = 20,
                StatGrowth = new() { ["STR"] = 1, ["DEX"] = 1, ["END"] = 2, ["INT"] = 2, ["SPR"] = 2 },
                HpPerLevel = 6,
                ManaPerLevel = 5,
                ForbiddenClasses = _nonAmatoForbidden
            },
            [PlayerRace.Rotuka] = new RaceProfile
            {
                Race = PlayerRace.Rotuka,
                BaseStatBonus = new() { ["STR"] = 5, ["DEX"] = 2, ["END"] = 7, ["INT"] = -2, ["SPR"] = -3 },
                BaseHpBonus = 25,
                BaseManaBonus = 0,
                StatGrowth = new() { ["STR"] = 3, ["DEX"] = 2, ["END"] = 2, ["INT"] = 0, ["SPR"] = 1 },
                HpPerLevel = 7,
                ManaPerLevel = 1,
                ForbiddenClasses = _nonAmatoForbidden
            },
            [PlayerRace.Iymva] = new RaceProfile
            {
                Race = PlayerRace.Iymva,
                BaseStatBonus = new() { ["STR"] = -1, ["DEX"] = 5, ["END"] = 2, ["INT"] = 1, ["SPR"] = 0 },
                BaseHpBonus = 10,
                BaseManaBonus = 5,
                StatGrowth = new() { ["STR"] = 1, ["DEX"] = 3, ["END"] = 1, ["INT"] = 1, ["SPR"] = 1 },
                HpPerLevel = 4,
                ManaPerLevel = 3,
                ForbiddenClasses = _nonAmatoForbidden
            },
            [PlayerRace.Zalu] = new RaceProfile
            {
                Race = PlayerRace.Zalu,
                BaseStatBonus = new() { ["STR"] = 0, ["DEX"] = 0, ["END"] = 3, ["INT"] = 0, ["SPR"] = 0 },
                BaseHpBonus = 10,
                BaseManaBonus = 0,
                StatGrowth = new() { ["STR"] = 2, ["DEX"] = 2, ["END"] = 2, ["INT"] = 1, ["SPR"] = 1 },
                HpPerLevel = 5,
                ManaPerLevel = 2,
                ForbiddenClasses = _nonAmatoForbidden
            },
            [PlayerRace.Gavon] = new RaceProfile
            {
                Race = PlayerRace.Gavon,
                BaseStatBonus = new() { ["STR"] = -3, ["DEX"] = 2, ["END"] = 1, ["INT"] = 4, ["SPR"] = 5 },
                BaseHpBonus = 5,
                BaseManaBonus = 25,
                StatGrowth = new() { ["STR"] = 1, ["DEX"] = 1, ["END"] = 1, ["INT"] = 2, ["SPR"] = 2 },
                HpPerLevel = 3,
                ManaPerLevel = 6,
                ForbiddenClasses = _nonAmatoForbidden
            },
            [PlayerRace.Gamato] = new RaceProfile
            {
                Race = PlayerRace.Gamato,
                BaseStatBonus = new() { ["STR"] = 1, ["DEX"] = 1, ["END"] = 4, ["INT"] = 1, ["SPR"] = 1 },
                BaseHpBonus = 15,
                BaseManaBonus = 5,
                StatGrowth = new() { ["STR"] = 2, ["DEX"] = 2, ["END"] = 2, ["INT"] = 1, ["SPR"] = 1 },
                HpPerLevel = 5,
                ManaPerLevel = 3,
                ForbiddenClasses = _nonAmatoForbidden
            },
            [PlayerRace.Amato] = new RaceProfile
            {
                Race = PlayerRace.Amato,
                BaseStatBonus = new() { ["STR"] = 1, ["DEX"] = 2, ["END"] = 4, ["INT"] = 1, ["SPR"] = 2 },
                BaseHpBonus = 15,
                BaseManaBonus = 10,
                StatGrowth = new() { ["STR"] = 1, ["DEX"] = 2, ["END"] = 1, ["INT"] = 2, ["SPR"] = 2 },
                HpPerLevel = 5,
                ManaPerLevel = 4,
                ForbiddenClasses = _amatoForbidden
            }
        };
    }
}
