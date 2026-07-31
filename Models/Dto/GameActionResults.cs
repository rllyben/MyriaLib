namespace MyriaLib.Models.Dto
{
    public record GatherActionResult(bool Success, string? Reason, string? ItemId, int Amount, long SkillXpGained, string? JobId, int RemainingGathers = -1);
    public record CraftActionResult(bool Success, string? Reason, string? ItemId, int Amount, long SkillXpGained, string? JobId);
    public record UpgradeActionResult(bool Success, string? Reason, string? ItemId, int UpgradeLevel, long SkillXpGained, string? JobId);
    public record StartCombatResult(bool Success, string? Reason, string? MonsterName, int MonsterHp, int MonsterMaxHp, int MonsterLevel = 0);
    public record NpcShopBuyResult(bool Success, string? Reason, long TotalCost, int Quantity);
    public record EquipItemResult(bool Success, string? Reason);
    public record CombatLogMessage(string Key, string[] Args);
    public record CombatTurnResult(
        bool Success,
        List<CombatLogMessage> LogEntries,
        int CharacterHp,
        int MonsterHp,
        string Phase,
        bool Finished,
        bool CharacterWon,
        long XpGained,
        List<string> LootItemIds);

    public record GroupCombatantState(string Name, int Hp, int MaxHp, bool IsAlive, int Level = 0);
    public record StartGroupCombatResult(
        bool Success,
        string? Reason,
        List<GroupCombatantState> Characters,
        List<GroupCombatantState> Monsters,
        string CurrentTurnCharacterName);
    public record GroupCombatSnapshot(
        List<GroupCombatantState> Characters,
        List<GroupCombatantState> Monsters,
        string CurrentTurnCharacterName,
        bool Finished,
        bool CharactersWon,
        List<CombatLogMessage> LogEntries);
}
