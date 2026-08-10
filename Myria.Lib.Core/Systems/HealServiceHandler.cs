using Myria.Lib.Core.Entities.Maps;
using Myria.Lib.Core.Entities.NPCs;
using Myria.Lib.Core.Entities.Characters;
using Myria.Lib.Core.Systems.Interfaces;

namespace Myria.Lib.Core.Systems
{
    public sealed class HealServiceHandler : INpcServiceHandler
    {
        public string ServiceId => "heal";

        public bool CanUse(Character character, Npc npc, Room room) => character.IsAlive;

        public NpcActionResult Execute(Character character, Npc npc, Room room, object? args = null)
        {
            // Example pricing logic (change as you like)
            int cost = 0;

            if (!character.Money.CanAfford(cost))
                return new NpcActionResult
                {
                    Success = false,
                    MessageKey = "npc.healer.notEnoughMoney",
                    MessageArgs = new object[] { cost }
                };

            character.Money.TrySpend(cost);

            int hpBefore = character.CurrentHealth;
            int mpBefore = character.CurrentMana;

            character.CurrentHealth = character.MaxHealth;
            character.CurrentMana = character.MaxMana;

            return new NpcActionResult
            {
                Success = true,
                MessageKey = "npc.healer.healed"
            };

        }

    }

}
