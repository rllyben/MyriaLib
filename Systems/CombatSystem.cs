using MyriaLib.Entities.Monsters;
using MyriaLib.Entities.Players;
using MyriaLib.Entities.Skills;
using MyriaLib.Systems.Interfaces;
namespace MyriaLib.Systems
{
    public static class CombatSystem
    {
        private class PendingAction
        {
            public int TurnsRemaining { get; set; }
            public Action Execute { get; set; }
        }

        private static readonly Random _random = new();
        public static bool TryHit(ICombatant attacker, ICombatant defender)
        {
            float aim = attacker.TotalAim;
            float evasion = defender.TotalEvasion;

            if (aim >= evasion)
            {
                return true; // Guaranteed hit
            }
            float hitChance = aim / evasion;
            float roll = (float)_random.NextDouble();

            return roll <= hitChance; // True = hit, False = miss
        }
        public static int CalculateDamage(ICombatant attacker, ICombatant defender)
        {
            float atk = attacker.TotalPhysicalAttack;
            float matk = attacker.TotalMagicAttack;
            float def = defender.TotalPhysicalDefense;
            float mdef = defender.TotalMagicDefense;

            if (!TryHit(attacker, defender))
            {
                return 0;
            }

            float pdmg = atk * (atk / (atk + def));
            float mdmg = matk * (matk / (matk + mdef));
            float dmg = Math.Max(pdmg, mdmg);
            if (dmg < 1)
                dmg = 1;
            // Check for block
            float blockRoll = (float)_random.NextDouble();
            if (blockRoll < defender.GetBlockChance())
            {
                dmg /= 2;
            }

            return (int)dmg;
        }

    }

}