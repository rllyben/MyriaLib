namespace Myria.Lib.Core.Models.Settings
{
    /// <summary>
    /// Stores all player-configurable keybindings as WPF Key enum name strings
    /// (e.g. "W", "Escape", "D1", "OemCaret").
    /// Serialized as part of settings.json.
    /// </summary>
    public class KeybindingSettings
    {
        // ── Room movement ─────────────────────────────────────────────────────────
        public string MoveNorth { get; set; } = "W";
        public string MoveSouth { get; set; } = "S";
        public string MoveWest  { get; set; } = "A";
        public string MoveEast  { get; set; } = "D";

        // ── In-game navigation (disabled during fights) ───────────────────────────
        public string OpenInventory { get; set; } = "R";
        public string OpenCharacter { get; set; } = "C";
        public string OpenSkills    { get; set; } = "T";
        public string OpenQuests    { get; set; } = "J";
        public string OpenMap       { get; set; } = "M";
        public string OpenSettings  { get; set; } = "Escape";

        // ── Room actions ──────────────────────────────────────────────────────────
        public string StartFight  { get; set; } = "F";
        public string StartGather { get; set; } = "G";

        // ── Combat actions ────────────────────────────────────────────────────────
        public string FightAttack  { get; set; } = "OemCaret"; // ^ key
        public string FightSkill1  { get; set; } = "D1";
        public string FightSkill2  { get; set; } = "D2";
        public string FightSkill3  { get; set; } = "D3";
        public string FightSkill4  { get; set; } = "D4";
        public string FightSkill5  { get; set; } = "D5";
        public string FightSkill6  { get; set; } = "D6";
        public string FightSkill7  { get; set; } = "D7";
        public string FightSkill8  { get; set; } = "D8";
        public string FightSkill9  { get; set; } = "D9";
        public string FightSkill10 { get; set; } = "D0";
    }
}
