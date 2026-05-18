using MyriaLib.Systems.Enums;

namespace MyriaLib.Entities.Players
{
    public class ClassProfile
    {
        public PlayerClass Class { get; set; }
        public Dictionary<string, int> StatGrowth { get; set; } = new();
        public int HpPerLevel { get; set; }
        public int ManaPerLevel { get; set; }

        public static Dictionary<PlayerClass, ClassProfile> All => new()
        {
            [PlayerClass.Archer] = new ClassProfile
            {
                Class = PlayerClass.Archer,
                StatGrowth = new()
                {
                    ["STR"] = 3,
                    ["DEX"] = 3,
                    ["END"] = 1,
                    ["INT"] = 1,
                    ["SPR"] = 1
                },
                HpPerLevel = 7,
                ManaPerLevel = 5
            },
            [PlayerClass.Hunter] = new ClassProfile
            {
                Class = PlayerClass.Hunter,
                StatGrowth = new()
                {
                    ["STR"] = 3,
                    ["DEX"] = 4,
                    ["END"] = 1,
                    ["INT"] = 1,
                    ["SPR"] = 1
                },
                HpPerLevel = 6,
                ManaPerLevel = 6
            },
            [PlayerClass.Knight] = new ClassProfile
            {
                Class = PlayerClass.Knight,
                StatGrowth = new()
                {
                    ["STR"] = 1,
                    ["DEX"] = 2,
                    ["END"] = 5,
                    ["INT"] = 1,
                    ["SPR"] = 2
                },
                HpPerLevel = 11,
                ManaPerLevel = 3
            },
            [PlayerClass.Fighter] = new ClassProfile
            {
                Class = PlayerClass.Fighter,
                StatGrowth = new()
                {
                    ["STR"] = 4,
                    ["DEX"] = 2,
                    ["END"] = 2,
                    ["INT"] = 1,
                    ["SPR"] = 2
                },
                HpPerLevel = 8,
                ManaPerLevel = 5
            },
            [PlayerClass.Barbarian] = new ClassProfile
            {
                Class = PlayerClass.Barbarian,
                StatGrowth = new()
                {
                    ["STR"] = 6,
                    ["DEX"] = 1,
                    ["END"] = 2,
                    ["INT"] = 0,
                    ["SPR"] = 1
                },
                HpPerLevel = 10,
                ManaPerLevel = 2
            },
            [PlayerClass.Cleric] = new ClassProfile
            {
                Class = PlayerClass.Cleric,
                StatGrowth = new()
                {
                    ["STR"] = 1,
                    ["DEX"] = 1,
                    ["END"] = 4,
                    ["INT"] = 1,
                    ["SPR"] = 4
                },
                HpPerLevel = 10,
                ManaPerLevel = 7
            },
            [PlayerClass.Rogue] = new ClassProfile
            {
                Class = PlayerClass.Rogue,
                StatGrowth = new()
                {
                    ["STR"] = 3,
                    ["DEX"] = 6,
                    ["END"] = 1,
                    ["INT"] = 1,
                    ["SPR"] = 1
                },
                HpPerLevel = 7,
                ManaPerLevel = 4
            },
            [PlayerClass.ElementalMage] = new ClassProfile
            {
                Class = PlayerClass.ElementalMage,
                StatGrowth = new()
                {
                    ["STR"] = 1,
                    ["DEX"] = 2,
                    ["END"] = 1,
                    ["INT"] = 5,
                    ["SPR"] = 3
                },
                HpPerLevel = 7,
                ManaPerLevel = 9
            },
            [PlayerClass.ArcanMage] = new ClassProfile
            {
                Class = PlayerClass.ArcanMage,
                StatGrowth = new()
                {
                    ["STR"] = 0,
                    ["DEX"] = 2,
                    ["END"] = 1,
                    ["INT"] = 6,
                    ["SPR"] = 2
                },
                HpPerLevel = 5,
                ManaPerLevel = 11
            },
            [PlayerClass.Druid] = new ClassProfile
            {
                Class = PlayerClass.Druid,
                StatGrowth = new()
                {
                    ["STR"] = 1,
                    ["DEX"] = 3,
                    ["END"] = 2,
                    ["INT"] = 2,
                    ["SPR"] = 3
                },
                HpPerLevel = 7,
                ManaPerLevel = 9
            },
            [PlayerClass.SoulsKnight] = new ClassProfile
            {
                Class = PlayerClass.SoulsKnight,
                StatGrowth = new()
                {
                    ["STR"] = 2,
                    ["DEX"] = 1,
                    ["END"] = 4,
                    ["INT"] = 2,
                    ["SPR"] = 4
                },
                HpPerLevel = 10,
                ManaPerLevel = 7
            },
            [PlayerClass.RunicMage] = new ClassProfile
            {
                Class = PlayerClass.RunicMage,
                StatGrowth = new()
                {
                    ["STR"] = 1,
                    ["DEX"] = 1,
                    ["END"] = 1,
                    ["INT"] = 4,
                    ["SPR"] = 4
                },
                HpPerLevel = 6,
                ManaPerLevel = 10
            }

        };

    }

}
