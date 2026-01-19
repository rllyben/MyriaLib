using MyriaLib.Entities.Maps;
using MyriaLib.Entities.NPCs;
using MyriaLib.Entities.Players;
using MyriaLib.Systems.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyriaLib.Systems
{
    public sealed class HealServiceHandler : INpcServiceHandler
    {
        public string ServiceId => "heal";

        public bool CanUse(Player player, Npc npc, Room room) => player.IsAlive;

        public NpcActionResult Execute(Player player, Npc npc, Room room, object? args = null)
        {
            // Example pricing logic (change as you like)
            int cost = 0;

            if (player.Money.Coins.TotalBronze < cost)
                return new NpcActionResult
                {
                    Success = false,
                    MessageKey = "npc.healer.notEnoughMoney",
                    MessageArgs = new object[] { cost }
                };

            player.Money.Coins.TrySpend(cost);

            int hpBefore = player.CurrentHealth;
            int mpBefore = player.CurrentMana;

            player.CurrentHealth = player.Stats.MaxHealth;
            player.CurrentMana = player.Stats.MaxMana;

            return new NpcActionResult
            {
                Success = true,
                MessageKey = "npc.healer.healed",
                HpRestored = player.CurrentHealth - hpBefore,
                MpRestored = player.CurrentMana - mpBefore,
                GoldChange = -cost
            };

        }

    }

}
