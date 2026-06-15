using MyriaLib.Entities.Characters;
using MyriaLib.Systems.Mods;

namespace MyriaLib.Entities.Items
{
    public class ConsumableItem : Item
    {
        public int HealAmount { get; set; }
        public int ManaRestore { get; set; }
        public string? UseEffect { get; set; }
        /// <summary>
        /// uses the item and grants its effects
        /// </summary>
        /// <param name="player">player character</param>
        public override void Use(Character character)
        {
            if (ModItemUseEffectRegistry.TryUse(this, character))
                return;

            ApplyBaseEffect(character);
        }

        public void ApplyBaseEffect(Character character)
        {
            if (HealAmount > 0)
                character.Heal(HealAmount, Id);

            if (ManaRestore > 0)
                character.RestoreMana(ManaRestore, Id);
        }

    }

}
