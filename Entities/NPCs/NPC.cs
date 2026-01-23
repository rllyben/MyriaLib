using MyriaLib.Entities.Items;
using MyriaLib.Services;
using MyriaLib.Systems;
using MyriaLib.Systems.Enums;

namespace MyriaLib.Entities.NPCs
{
    public class Npc
    {
        public string Id { get; set; } = "";
        public string NameKey { get; set; } = "";        // e.g. "game.npc.healer.name"
        public string DescriptionKey { get; set; } = ""; // e.g. "game.npc.healer.desc"

        public NpcType Type { get; set; }                // Healer, Shop, QuestGiver, etc.
        public List<string> Services { get; set; } = new(); // optional: "heal", "shop", "quests"
        public List<string> ItemNames { get; set; } = new();
        public List<Item> ItemRefs { get; set; } = new();
        public override string ToString()
        {
            return Localization.T(NameKey);
        }
        public void HealingAction()
        {
            UserAccoundService.CurrentCharacter.Heal(int.MaxValue, this.ToString());
            UserAccoundService.CurrentCharacter.RestoreMana(int.MaxValue, this.ToString());
        }
        public void BuyItem(Item item)
        {
            UserAccoundService.CurrentCharacter.Inventory.AddItem(item, UserAccoundService.CurrentCharacter, this.ToString());
        }
        public void SellItem(Item item)
        {

        }
        public void UpgradeItem(Item item)
        {

        }
        public void CraftItem(Item item)
        {

        }

    }

}
