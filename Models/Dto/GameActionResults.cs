namespace MyriaLib.Models.Dto
{
    public record GatherActionResult(bool Success, string? Reason, string? ItemId, int Amount, long SkillXpGained, string? JobId, int RemainingGathers = -1);
    public record CraftActionResult(bool Success, string? Reason, string? ItemId, int Amount, long SkillXpGained, string? JobId);
    public record UpgradeActionResult(bool Success, string? Reason, string? ItemId, int UpgradeLevel, long SkillXpGained, string? JobId);
    public record StartCombatResult(bool Success, string? Reason, string? MonsterName, int MonsterHp, int MonsterMaxHp, int MonsterLevel = 0);
    public record NpcShopBuyResult(bool Success, string? Reason, long TotalCost, int Quantity);
    public record NpcSellResult(bool Success, string? Reason, long TotalGain, int Quantity);
    public record HealActionResult(bool Success, string? Reason, int CharacterHp, int CharacterMaxHp, int CharacterMana, int CharacterMaxMana);
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
        List<string> LootItemIds,
        long RookieBonusXp = 0,
        int CharacterMana = 0,
        int CharacterMaxMana = 0);

    public record GroupCombatantState(string Name, int Hp, int MaxHp, bool IsAlive, int Level = 0, long XpGained = 0, long RookieBonusXp = 0, List<string>? LootItemIds = null, int Mana = 0, int MaxMana = 0);

    /// <summary>
    /// Authoritative character-level progression, fetched on demand after any action that
    /// might have changed it (combat win, etc.) instead of the client trying to replay the
    /// server's XP/level-up math itself, which drifts. Covers everything Character.LevelUp()
    /// mutates so a client behind by several levels catches up in one shot, not just Level/Xp.
    /// </summary>
    /// <summary>
    /// Authoritative kill/item objective progress for one active quest, fetched on demand
    /// after combat so the client's local Quest objects don't stay frozen at whatever they
    /// were when the character was last loaded — server-authoritative combat only ever
    /// ticks QuestManager's kill/item counters against the server's own session Character,
    /// never the client's local copy.
    /// </summary>
    public record QuestProgressState(
        string QuestId,
        Dictionary<int, int>    KillProgress,
        Dictionary<string, int> ItemProgress);

    public record CharacterProgressResult(
        int  Level,
        long Experience,
        long ExpForNextLvl,
        int  Strength,
        int  Dexterity,
        int  Endurance,
        int  Intelligence,
        int  Spirit,
        int  UnusedPoints,
        int  BaseHealth,
        int  BaseMana);
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
