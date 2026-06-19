namespace MyriaLib.Models.Dto
{
    public record GatherActionResult(bool Success, string? Reason, string? ItemId, int Amount, long SkillXpGained, string? JobId, int RemainingGathers = -1);
    public record CraftActionResult(bool Success, string? Reason, string? ItemId, int Amount, long SkillXpGained, string? JobId);
    public record UpgradeActionResult(bool Success, string? Reason, string? ItemId, int UpgradeLevel, long SkillXpGained, string? JobId);
    public record StartCombatResult(bool Success, string? Reason, string? MonsterName, int MonsterHp, int MonsterMaxHp, int MonsterLevel = 0);
    public record NpcShopBuyResult(bool Success, string? Reason, long TotalCost, int Quantity);
    public record CombatLogMessage(string Key, string[] Args);
    public record CombatTurnResult(
        bool Success,
        List<CombatLogMessage> LogEntries,
        int PlayerHp,
        int MonsterHp,
        string Phase,
        bool Finished,
        bool PlayerWon,
        long XpGained,
        List<string> LootItemIds);

    public record GroupCombatantState(string Name, int Hp, int MaxHp, bool IsAlive, int Level = 0);
    public record StartGroupCombatResult(
        bool Success,
        string? Reason,
        List<GroupCombatantState> Players,
        List<GroupCombatantState> Monsters,
        string CurrentTurnPlayerName);
    public record GroupCombatSnapshot(
        List<GroupCombatantState> Players,
        List<GroupCombatantState> Monsters,
        string CurrentTurnPlayerName,
        bool Finished,
        bool PlayersWon,
        List<CombatLogMessage> LogEntries);
}
