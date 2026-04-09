namespace MyriaLib.Entities.Players
{
    /// <summary>
    /// Holds the player's money. Enforces an optional capacity ceiling (upgradeable).
    /// Behaves like an inventory slot for currency — use TryAdd/TrySpend for mutations.
    /// </summary>
    public class MoneyBag
    {
        public Money Balance { get; set; } = new(0);
        public long Capacity { get; set; } = 300_000;
        public bool IsUnlimited => Capacity == long.MaxValue;

        public bool CanAfford(long cost)  => Balance.BronzeTotal >= cost;
        public bool CanHold(long amount)  => IsUnlimited || Balance.BronzeTotal + amount <= Capacity;

        public bool TryAdd(long amount)
        {
            if (!CanHold(amount)) return false;
            Balance += new Money(amount);
            return true;
        }

        public bool TrySpend(long amount)
        {
            if (!CanAfford(amount)) return false;
            Balance -= new Money(amount);
            return true;
        }

        public override string ToString() => Balance.ToString();
    }
}
