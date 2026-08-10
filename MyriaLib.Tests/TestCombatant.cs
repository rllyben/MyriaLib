using MyriaLib.Entities;
using MyriaLib.Entities.Items;
using MyriaLib.Systems.Interfaces;

namespace MyriaLib.Tests;

/// <summary>
/// Minimal ICombatant test double with every stat directly settable, so combat-formula
/// tests can pin exact inputs instead of reverse-engineering them through Character/Monster's
/// full stat-aggregation pipeline (race + class + gear + effects).
/// </summary>
public sealed class TestCombatant : ICombatant
{
    public string Name { get; set; } = "TestCombatant";
    public Stats Stats { get; set; } = new();

    public int CurrentHealth { get; set; } = 100;
    public int CurrentMana { get; set; } = 100;
    public int MaxHealth { get; set; } = 100;
    public int MaxMana { get; set; } = 100;

    public int TotalPhysicalAttack { get; set; }
    public int TotalPhysicalDefense { get; set; }
    public int TotalMagicAttack { get; set; }
    public int TotalMagicDefense { get; set; }
    public int TotalAim { get; set; }
    public int TotalEvasion { get; set; }
    public int TotalSTR { get; set; }
    public int TotalDEX { get; set; }
    public int TotalEND { get; set; }
    public int TotalINT { get; set; }
    public int TotalSPR { get; set; }
    public float CritChance { get; set; }
    public float BlockChance { get; set; }

    public void TakeDamage(int amount) => CurrentHealth = Math.Max(0, CurrentHealth - amount);
    public int DealPhysicalDamage() => TotalPhysicalAttack;
    public int DefandPhysical() => TotalPhysicalDefense;
    public float GetBlockChance() => BlockChance;
    public int GetBonusFromGear(Func<EquipmentItem, int> selector) => 0;
    public float GetBonusFromGear(Func<EquipmentItem, float> selector) => 0f;
}
