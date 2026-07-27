using System.Text.Json.Serialization;
using MyriaLib.Entities.Items;
using MyriaLib.Entities.Jobs;
using MyriaLib.Entities.Maps;
using MyriaLib.Entities.NPCs;
using MyriaLib.Entities.Skills;
using MyriaLib.Models.BaseModel;
using MyriaLib.Services.Builder;
using MyriaLib.Services.Manager;
using MyriaLib.Systems.Enums;
using MyriaLib.Systems.Events;
using MyriaLib.Utils;
namespace MyriaLib.Entities.Characters
{
    public class Character : CombatEntity
    {
        public event EventHandler<SkillLearnedEventArgs>? SkillLearned;
        public event EventHandler<XpGainedEventArgs>? XpGained;
        public event EventHandler<LevelUpEventArgs>? LeveledUp;
        public event EventHandler<HealthChangedEventArgs>? HealthChanged;
        public event EventHandler<ManaChangedEventArgs>? ManaChanged;
        public string Class { get; set; } = CharacterClass.Fighter;
        public string Race  { get; set; } = CharacterRace.Myralu;
        public int Level { get; set; } = 1;
        public long Experience { get; set; } = 0;
        public long ExpForNextLvl { get; set; }
        public int PotionTierAvailable { get; set; } = 1;
        public Inventory Inventory { get; set; } = new();
        public MoneyBag Money { get; set; } = new();
        public List<Skill> Skills { get; set; } = new();
        public List<Quest> ActiveQuests { get; set; } = new();
        public List<Quest> CompletedQuests { get; set; } = new();
        public Dictionary<string, RepeatRecord> RepeatableQuestRecords { get; set; } = new();
        public Dictionary<string, string> ModState { get; set; } = new();
        [JsonIgnore]
        public Room CurrentRoom { get; set; }
        public int CurrentRoomId { get; set; }

        // Runtime-only aggro level, reset at the start of each GroupCombatEncounter.
        // Determines which character monsters will focus on.
        [JsonIgnore]
        public float AggroLevel { get; set; }
        public int? LastHealerRoomId { get; set; } = null;
        public Dictionary<int, DateTime> RoomGatheringStatus { get; set; } = new();

        // ── Skill Fusion (WPF / Unity — physical/combat classes) ─────────────────
        /// <summary>All composite skills the player has created via fusion.</summary>
        public List<CompositeSkill> CompositeSkills { get; set; } = new();

        /// <summary>
        /// Which composite skill IDs are currently slotted for combat.
        /// Count is capped by <see cref="FusionSlotCount"/>.
        /// </summary>
        public List<string> ActiveCompositeSkillIds { get; set; } = new();

        /// <summary>Composite skills stashed per class; restored when the player switches back.</summary>
        public Dictionary<string, List<CompositeSkill>> StashedCompositeSkills { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Level breakpoints shared by <see cref="FusionSlotCount"/> and <see cref="SkillSlotCount"/>
        /// (both use the same curve by default). Base is 1 slot; each entry raises the cap once
        /// Level reaches it. Pass entries sorted ascending by level — mirrors the shape and the
        /// same ordering assumption as JobXpService.GatherBonusThresholds/UpgradeGates. A mod or a
        /// differently-paced game can replace this instead of being stuck with a hardcoded curve.
        /// </summary>
        public static (int Level, int Slots)[] SkillSlotBreakpoints { get; set; } =
        {
            (3, 2), (9, 3), (18, 4), (27, 5), (36, 6), (45, 7), (54, 8), (63, 9), (72, 10)
        };

        private static int ResolveSlotCount(int level)
        {
            int slots = 1;
            foreach (var (breakLevel, breakSlots) in SkillSlotBreakpoints)
                if (level >= breakLevel) slots = breakSlots;
            return slots;
        }

        /// <summary>Maximum number of fusion skills the player can have active, based on level.</summary>
        [JsonIgnore]
        public int FusionSlotCount => ResolveSlotCount(Level);

        // ── Skill Combination (combining 2–5 learned base skills) ─────────────────
        /// <summary>All combined skills the player has created by pairing their learned skills.</summary>
        public List<CombinedSkill> CombinedSkills { get; set; } = new();

        /// <summary>Combined skills stashed per class; restored when the player switches back.</summary>
        public Dictionary<string, List<CombinedSkill>> StashedCombinedSkills { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        // ── Combat Skill Slots ────────────────────────────────────────────────────
        /// <summary>
        /// The ordered list of skills configured for the combat skill bar.
        /// Only slotted skills appear during a fight. Capped by <see cref="SkillSlotCount"/>.
        /// </summary>
        public List<SkillSlot> SkillSlots { get; set; } = new();

        /// <summary>
        /// Maximum number of skills the player can slot for combat, based on level.
        /// See <see cref="SkillSlotBreakpoints"/> for the level curve.
        /// </summary>
        [JsonIgnore]
        public int SkillSlotCount => ResolveSlotCount(Level);

        // ── Race ─────────────────────────────────────────────────────────────────
        /// <summary>
        /// False for characters created before the race-selection UI existed.
        /// The loading path uses this to trigger a one-time migration.
        /// </summary>
        public bool RaceSelected { get; set; } = false;

        // ── Class XP ─────────────────────────────────────────────────────────────
        public Dictionary<string, long> ClassXp { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public DateTime LastClassPenaltyApplied { get; set; } = DateTime.MinValue;
        /// <summary>UTC timestamp of the last class switch; DateTime.MinValue = never switched (no cooldown).</summary>
        public DateTime LastClassChanged { get; set; } = DateTime.MinValue;

        protected override int ExtraSTR => ClassManager.GetClassBonusForStat(this, "STR");
        protected override int ExtraDEX => ClassManager.GetClassBonusForStat(this, "DEX");
        protected override int ExtraEND => ClassManager.GetClassBonusForStat(this, "END");
        protected override int ExtraINT => ClassManager.GetClassBonusForStat(this, "INT");
        protected override int ExtraSPR => ClassManager.GetClassBonusForStat(this, "SPR");
        protected override int ExtraBaseHealth => ClassManager.GetClassHpBonus(this);
        protected override int ExtraBaseMana   => ClassManager.GetClassManaBonus(this);

        // ── Jobs ─────────────────────────────────────────────────────────────────
        public string? ActiveJobId { get; set; }
        public List<CharacterJob> Jobs { get; set; } = new();
        /// <summary>UTC timestamp of the last time the player switched to a new active job.</summary>
        public DateTime LastJobChanged { get; set; } = DateTime.MinValue;

        // ── Runic Magic (WPF / Unity — magic classes) ────────────────────────────
        /// <summary>All runes the player knows, including those gained via word combinations.</summary>
        public List<CompositeRune> KnownRunes { get; set; } = new();

        /// <summary>The player's runic word discovery and translation state.</summary>
        public List<CharacterRuneWordEntry> RuneDictionary { get; set; } = new();

        // Add inventory, experience, commands, etc.
        public Character(string name, Stats stats)
        {
            Name = name;
            Stats = stats;
            CurrentHealth = MaxHealth;
            CurrentMana = MaxMana;
            ExpForNextLvl = (long)(Math.Pow(Level, 2)) * 50;
        }
        public void GainXp(long amount)
        {
            if (amount <= 0) return;

            Experience += amount;

            // Level-up loop in case you gain a lot at once
            while (Experience >= ExpForNextLvl)
            {
                int old = Level;
                Experience -= ExpForNextLvl;
                LevelUp(); // your existing method (or implement it)
                LeveledUp?.Invoke(this, new LevelUpEventArgs(old, Level));
            }

            XpGained?.Invoke(this, new XpGainedEventArgs(
                amount,
                Experience,
                ExpForNextLvl
            ));

        }
        public override void TakeDamage(int amount)
        {
            int old = CurrentHealth;
            base.TakeDamage(amount);
            if (CurrentHealth != old)
                HealthChanged?.Invoke(this, new HealthChangedEventArgs(old, CurrentHealth, null));
        }

        public int ApplyDamage(int amount, string? source = null)
        {
            if (amount <= 0) return 0;

            int old = CurrentHealth;
            int newValue = Math.Max(0, CurrentHealth - amount);
            CurrentHealth = newValue;

            int actual = old - newValue;
            if (actual != 0)
                HealthChanged?.Invoke(this, new HealthChangedEventArgs(old, newValue, source));

            return actual;
        }

        public override int Heal(int amount, string? source = null)
        {
            if (amount <= 0) return 0;

            int old = CurrentHealth;
            int max = MaxHealth;
            if (amount > max)
                amount = max;
            int newValue = Math.Min(max, CurrentHealth + amount);
            CurrentHealth = newValue;

            int actual = newValue - old;
            if (actual != 0)
                HealthChanged?.Invoke(this, new HealthChangedEventArgs(old, newValue, source));

            return actual;
        }

        /// <summary>
        /// Sets health to an absolute value (e.g. server-authoritative multiplayer combat
        /// results) and fires <see cref="HealthChanged"/> if it actually changed — unlike
        /// assigning <see cref="CurrentHealth"/> directly, which UI listeners never see.
        /// </summary>
        public void SetHealth(int newValue, string? source = null)
        {
            int old = CurrentHealth;
            newValue = Math.Clamp(newValue, 0, MaxHealth);
            if (newValue == old) return;
            CurrentHealth = newValue;
            HealthChanged?.Invoke(this, new HealthChangedEventArgs(old, newValue, source));
        }

        /// <summary>Mana counterpart to <see cref="SetHealth"/>.</summary>
        public void SetMana(int newValue, string? source = null)
        {
            int old = CurrentMana;
            newValue = Math.Clamp(newValue, 0, MaxMana);
            if (newValue == old) return;
            CurrentMana = newValue;
            ManaChanged?.Invoke(this, new ManaChangedEventArgs(old, newValue, source));
        }

        public int SpendMana(int amount, string? source = null)
        {
            if (amount <= 0) return 0;

            int old = CurrentMana;
            int newValue = Math.Max(0, CurrentMana - amount);
            CurrentMana = newValue;

            int actual = old - newValue;
            if (actual != 0)
                ManaChanged?.Invoke(this, new ManaChangedEventArgs(old, newValue, source));

            return actual;
        }

        public override int RestoreMana(int amount, string? source = null)
        {
            if (amount <= 0) return 0;
            int old = CurrentMana;
            int max = MaxMana;
            if (amount > max)
                amount = max;
            int newValue = Math.Min(max, CurrentMana + amount);
            CurrentMana = newValue;

            int actual = newValue - old;
            if (actual != 0)
                ManaChanged?.Invoke(this, new ManaChangedEventArgs(old, newValue, source));

            return actual;
        }
        /// <summary>
        /// Applies an death penalty if the player dies
        /// </summary>
        public void ApplyDeathXpPenalty()
        {
            long penalty = (long)(ExpForNextLvl * 0.01f);
            long actualLoss = Math.Min(penalty, Experience);
            Experience -= actualLoss;
        }
        /// <summary>
        /// equips an equipment item to its slot
        /// </summary>
        /// <param name="item">item to equip</param>
        public void Equip(EquipmentItem item)
        {
            switch (item.SlotType)
            {
                case EquipmentType.Weapon:
                    if (WeaponSlot != null) Inventory.AddItem(WeaponSlot, this);
                    WeaponSlot = item;
                    break;
                case EquipmentType.Armor:
                    if (ArmorSlot != null) Inventory.AddItem(ArmorSlot, this);
                    ArmorSlot = item;
                    break;
                case EquipmentType.Accessory:
                    if (AccessorySlot != null) Inventory.AddItem(AccessorySlot, this);
                    AccessorySlot = item;
                    break;
            }

        }
        /// <summary>
        /// updates stats for an Level up
        /// </summary>
        public void LevelUp()
        {
            var profile = RaceProfile.All[Race];

            Level++;
            Stats.Strength += profile.StatGrowth["STR"];
            Stats.Dexterity += profile.StatGrowth["DEX"];
            Stats.Endurance += profile.StatGrowth["END"];
            Stats.Intelligence += profile.StatGrowth["INT"];
            Stats.Spirit += profile.StatGrowth["SPR"];
            Stats.UnusedPoints++;

            Stats.BaseHealth += profile.HpPerLevel;
            Stats.BaseMana += profile.ManaPerLevel;

            CurrentHealth = MaxHealth;
            CurrentMana = MaxMana;
            ExpForNextLvl = (long)(Math.Pow(Level, 2)) * 50;
        }
        public bool LearnSkill(Skill skill)
        {
            if (skill == null) return false;

            // prevent duplicates (use Id if you have it)
            if (Skills.Any(s => s.Id == skill.Id))
                return false;

            Skills.Add(skill);

            SkillLearned?.Invoke(this, new SkillLearnedEventArgs(skill));
            return true;
        }

        /// <summary>
        /// Returns true if the player has a tool that enables the given gathering type —
        /// checks both the inventory bag and the equipped weapon slot.
        /// </summary>
        public bool HasToolFor(GatheringType type)
        {
            if (type == GatheringType.Herb) return true;
            return Inventory.Items.Any(i => i.ToolType == type)
                || WeaponSlot?.ToolType == type;
        }

        /// <summary>
        /// Reverts any quest that is marked Completed but whose requirements are not actually met.
        /// Corrects saves that were written while the completion bug was active.
        /// </summary>
        public void ValidateQuestStatuses()
        {
            foreach (var quest in ActiveQuests.Where(q => q.Status == QuestStatus.Completed))
            {
                bool killsDone = quest.RequiredKills.All(rk =>
                    quest.KillProgress.TryGetValue(rk.Key, out int kills) && kills >= rk.Value);
                bool itemsDone = quest.RequiredItems.All(ri =>
                    quest.ItemProgress.TryGetValue(ri.Key, out int items) && items >= ri.Value);

                if (!killsDone || !itemsDone)
                    quest.Status = QuestStatus.InProgress;
            }
        }

        /// <summary>
        /// Recalculates unused stat points based on level and spent points.
        /// Useful for imported characters or save migrations.
        /// </summary>
        public void RecalculateUnusedPoints()
        {
            // 1 point per level starting from level 2 (level 1 has 0 points)
            int totalPointsEarned = Math.Max(0, Level - 1);

            int pointsSpent = Stats.StrengthBonus + Stats.DexterityBonus + Stats.EnduranceBonus
                            + Stats.IntelligenceBonus + Stats.SpiritBonus;

            Stats.UnusedPoints = Math.Max(0, totalPointsEarned - pointsSpent);
        }

    }

}
