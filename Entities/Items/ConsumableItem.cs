using MyriaLib.Entities.Characters;

namespace MyriaLib.Entities.Items
{
    public class ConsumableItem : Item
    {
        public int HealAmount { get; set; }
        public int ManaRestore { get; set; }
        /// <summary>
        /// uses the item and grants its effects
        /// </summary>
        /// <param name="player">player character</param>
        public override void Use(Character character)
        {
            if (HealAmount > 0)
            {
                int healed = Math.Min(HealAmount, character.MaxHealth - character.CurrentHealth);
                character.CurrentHealth += healed;
            }

            if (ManaRestore > 0)
            {
                int restored = Math.Min(ManaRestore, character.MaxMana - character.CurrentMana);
                character.CurrentMana += restored;
            }

        }

    }

}
