using MyriaLib.Systems.Interfaces;
using MyriaLib.Entities.Items;

namespace MyriaLib.Entities
{
    public abstract class CombatEntity : ICombatant
    {
        public string Name { get; set; }
        public Stats Stats { get; set; } = new();
        public int CurrentHealth { get; set; }
        public int CurrentMana { get; set; }

        // Full stat totals — class base + player investment + gear bonuses
        public int TotalSTR => Stats.TotalStrength + GetBonusFromGear(g => g.Bonuses.STR);
        public int TotalDEX => Stats.TotalDexterity + GetBonusFromGear(g => g.Bonuses.DEX);
        public int TotalEND => Stats.TotalEndurance + GetBonusFromGear(g => g.Bonuses.END);
        public int TotalINT => Stats.TotalIntelligence + GetBonusFromGear(g => g.Bonuses.INT);
        public int TotalSPR => Stats.TotalSpirit + GetBonusFromGear(g => g.Bonuses.SPR);

        // MaxHealth/MaxMana live here so gear HP/MP bonuses are included
        public int MaxHealth => Stats.BaseHealth + TotalEND * 5 + GetBonusFromGear(g => g.Bonuses.HP);
        public int MaxMana   => Stats.BaseMana   + TotalSPR * 5 + GetBonusFromGear(g => g.Bonuses.MP);

        public bool IsAlive => CurrentHealth > 0;

        public EquipmentItem? WeaponSlot    { get; set; }
        public EquipmentItem? ArmorSlot     { get; set; }
        public EquipmentItem? AccessorySlot { get; set; }

        public int TotalPhysicalAttack  => (TotalSTR * 2 + TotalEND) + GetBonusFromGear(g => g.Bonuses.ATK);
        public int TotalPhysicalDefense => (TotalEND * 2 + TotalSTR) + GetBonusFromGear(g => g.Bonuses.DEF);
        public int TotalMagicAttack     => (TotalINT * 2 + TotalSPR) + GetBonusFromGear(g => g.Bonuses.MATK);
        public int TotalMagicDefense    => (TotalSPR * 2 + TotalINT) + GetBonusFromGear(g => g.Bonuses.MDEF);
        public int TotalAim             => TotalDEX + GetBonusFromGear(g => g.Bonuses.Aim);
        public int TotalEvasion         => (int)(TotalDEX * 0.85f) + GetBonusFromGear(g => g.Bonuses.Evasion);

        public float CritChance =>
            GetBonusFromGear(g => g.Bonuses.DEX) * 0.1f + GetBonusFromGear(g => g.Bonuses.Crit);

        public float BlockChance
        {
            get
            {
                float blockRaw = (GetBonusFromGear(g => g.Bonuses.END) * 0.3f)
                               + (GetBonusFromGear(g => g.Bonuses.INT) * 0.2f)
                               + (GetBonusFromGear(g => g.Bonuses.STR) * 0.1f)
                               + GetBonusFromGear(g => g.Bonuses.Block);
                return MathF.Min(blockRaw * 0.01f, 0.75f);
            }
        }

        public virtual void TakeDamage(int amount)
        {
            CurrentHealth = Math.Max(0, CurrentHealth - amount);
        }

        public virtual int DealPhysicalDamage() => TotalPhysicalAttack;
        public virtual int DefandPhysical()      => TotalPhysicalDefense;
        public virtual float GetBlockChance()    => BlockChance;

        public int GetBonusFromGear(Func<EquipmentItem, int> selector)
        {
            int total = 0;
            if (WeaponSlot    != null) total += selector(WeaponSlot);
            if (ArmorSlot     != null) total += selector(ArmorSlot);
            if (AccessorySlot != null) total += selector(AccessorySlot);
            return total;
        }

        public float GetBonusFromGear(Func<EquipmentItem, float> selector)
        {
            float total = 0;
            if (WeaponSlot    != null) total += selector(WeaponSlot);
            if (ArmorSlot     != null) total += selector(ArmorSlot);
            if (AccessorySlot != null) total += selector(AccessorySlot);
            return total;
        }
    }
}
