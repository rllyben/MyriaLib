using MyriaLib.Entities.Monsters;
using MyriaLib.Entities.Characters;
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

        private static readonly Random _random = Random.Shared;
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

        // Returns damage and whether it was a critical hit (player-facing attacks only).
        // Crits: 10% base + DEX/500 bonus, capped at 30%; deal 1.75× damage.
        public static (int Damage, bool IsCritical) CalculateDamageWithCrit(ICombatant attacker, ICombatant defender)
        {
            float atk  = attacker.TotalPhysicalAttack;
            float matk = attacker.TotalMagicAttack;
            float def  = defender.TotalPhysicalDefense;
            float mdef = defender.TotalMagicDefense;

            if (!TryHit(attacker, defender)) return (0, false);

            float pdmg = atk  * (atk  / (atk  + def));
            float mdmg = matk * (matk / (matk + mdef));
            float dmg  = Math.Max(pdmg, mdmg);
            if (dmg < 1) dmg = 1;

            if ((float)_random.NextDouble() < defender.GetBlockChance())
                dmg /= 2;

            float critChance = Math.Min(0.30f, 0.10f + attacker.TotalDEX / 500f);
            bool  isCrit     = (float)_random.NextDouble() < critChance;
            if (isCrit) dmg *= 1.75f;

            return ((int)dmg, isCrit);
        }

    }

}