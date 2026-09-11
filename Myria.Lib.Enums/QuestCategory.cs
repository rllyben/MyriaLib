namespace Myria.Lib.Core.Systems.Enums
{
    /// <summary>
    /// Broad grouping shown as section headers in the quest list UI (Q14). A small, fixed set
    /// (unlike <see cref="ItemRarity"/>/<see cref="GatheringType"/>, which were converted to
    /// string-constant classes specifically to support mod-added values) - nothing in the design
    /// calls for players/mods to add new categories, so a plain closed enum matches
    /// <see cref="QuestStatus"/>/<see cref="RoomRequirementType"/>'s convention instead.
    /// </summary>
    public enum QuestCategory
    {
        Main,
        Side,
        Faction
    }
}
