using MyriaLib.Entities.Items;
using MyriaLib.Entities.Monsters;
using MyriaLib.Entities.Characters;
using MyriaLib.Entities.Skills;
using MyriaLib.Services.Builder;
using MyriaLib.Services.Manager;
using MyriaLib.Systems.Enums;
using MyriaLib.Systems.Events;
using MyriaLib.Systems.Interfaces;

namespace MyriaLib.Systems
{
    public sealed class CombatEncounter
    {
        public Character Character { get; }
        public Monster Enemy { get; }

        public CombatPhase Phase { get; private set; } = CombatPhase.PlayerTurn;

        public int TurnsUntilActionExecutes { get; private set; } = 0;
        public int RecoveryTurnsRemaining { get; private set; } = 0;

        public List<CombatLogEntry> Log { get; } = new();

        private Action? _pendingAction;
        private Dictionary<string, int> _dropnumbers = new Dictionary<string, int>();
        private List<Item> _drops = new List<Item>();
        public bool InventoryFull { get; set; } = false;
        public event EventHandler<MonsterKilledEventArgs>? MonsterKilled;

        public CombatEncounter(Character character, Monster enemy)
        {
            Character = character;
            Enemy = enemy;
            Enemy.ResetHealth(); // if you do that elsewhere, remove
            Log.Add(new CombatLogEntry("pg.fight.log.start", enemy.Name));
            MonsterKilled += UpdateQuestProgress;
        }

        public void PlayerAttack()
        {
            if (Phase != CombatPhase.PlayerTurn) return;

            var (dmg, isCrit) = CombatSystem.CalculateDamageWithCrit(Character, Enemy);
            if (dmg <= 0)
                Log.Add(new CombatLogEntry("pg.fight.log.miss", Character.Name));
            else
            {
                Enemy.TakeDamage(dmg);
                Log.Add(isCrit
                    ? new CombatLogEntry("pg.fight.log.critHit", Character.Name, dmg)
                    : new CombatLogEntry("pg.fight.log.hit", Character.Name, dmg));
            }

            EndPlayerAction();
        }

        public bool PlayerBeginCast(Skill skill)
        {
            if (Phase != CombatPhase.PlayerTurn) return false;
            if (Character.CurrentMana < skill.ManaCost)
            {
                Log.Add(new CombatLogEntry("pg.fight.log.nomana"));
                return false;
            }

            if (skill.CastTime > 0)
            {
                Phase = CombatPhase.Casting;
                TurnsUntilActionExecutes = skill.CastTime;

                _pendingAction = () =>
                {
                    ExecuteSkill(skill);
                    RecoveryTurnsRemaining = skill.RecoveryTime;
                };

                Log.Add(new CombatLogEntry("pg.fight.log.beginCast", Character.Name, skill.Name));
            }
            else
            {
                ExecuteSkill(skill);
                RecoveryTurnsRemaining = skill.RecoveryTime;
                EndPlayerAction();
            }

            return true;
        }

        /// <summary>
        /// Use a consumable item in combat. Consumes the item immediately;
        /// the enemy gets one free attack in the recovery turn before the player can act again.
        /// </summary>
        public bool PlayerUseItem(ConsumableItem item)
        {
            if (Phase != CombatPhase.PlayerTurn) return false;

            item.Use(Character);
            Character.Inventory.RemoveItem(item);
            Log.Add(new CombatLogEntry("pg.fight.log.usedItem", Character.Name, item.Name));

            RecoveryTurnsRemaining = 1;
            EndPlayerAction();
            return true;
        }

        public void Tick() // advances time by 1 "round"
        {
            if (Phase == CombatPhase.Finished) return;

            // casting countdown
            if (Phase == CombatPhase.Casting)
            {
                TurnsUntilActionExecutes--;
                if (TurnsUntilActionExecutes <= 0)
                {
                    _pendingAction?.Invoke();
                    _pendingAction = null;

                    if (Enemy.IsAlive) EnemyTurn();
                    else FinishPlayerWon();
                }
                return;
            }

            // recovery countdown
            if (Phase == CombatPhase.Recovery)
            {
                RecoveryTurnsRemaining--;
                if (RecoveryTurnsRemaining <= 0)
                    Phase = CombatPhase.PlayerTurn;
                else
                    EnemyTurn();
            }

        }

        private void ExecuteSkill(Skill skill)
        {
            Character.SpendMana(skill.ManaCost);

            switch (skill.Target)
            {
                case SkillTarget.Self:
                    ExecuteSkillOnSelf(skill);
                    break;

                case SkillTarget.AllEnemies:
                    ExecuteSkillOnEnemy(skill, Enemy);
                    break;

                case SkillTarget.SingleAlly:
                    // In solo combat, SingleAlly targets the only ally: the player themselves
                    ExecuteSkillOnSelf(skill);
                    break;

                case SkillTarget.SingleEnemy:
                default:
                    ExecuteSkillOnEnemy(skill, Enemy);
                    break;
            }

            // Invoke optional code-defined effect last (buffs, status effects, etc.)
            skill.Effect?.Invoke(Character, Enemy);
        }

        private void ExecuteSkillOnSelf(Skill skill)
        {
            if (skill.IsHealing)
            {
                int baseStat = ResolveSkillBaseStat(skill);
                int heal = (int)(baseStat * skill.ScalingFactor);
                int healed = Math.Min(heal, Character.MaxHealth - Character.CurrentHealth);
                Character.Heal(healed);
                Log.Add(new CombatLogEntry("pg.fight.log.heal", Character.Name, healed));
            }
            // Non-healing Self skills (buffs) rely entirely on skill.Effect invoked after this.
        }

        private void ExecuteSkillOnEnemy(Skill skill, ICombatant target)
        {
            if (skill.IsHealing) return;

            int baseStat = ResolveSkillBaseStat(skill);
            float raw = baseStat * skill.ScalingFactor;
            float def = skill.Type == SkillType.Physical
                ? target.TotalPhysicalDefense
                : target.TotalMagicDefense;
            int dmg = Math.Max(1, (int)(raw * (raw / (raw + def))));
            target.TakeDamage(dmg);
            Log.Add(new CombatLogEntry("pg.fight.log.skillHit", Character.Name, skill.Name, dmg));
        }

        private int ResolveSkillBaseStat(Skill skill)
        {
            // same mapping you already have :contentReference[oaicite:4]{index=4}
            return skill.StatToScaleFrom.ToUpper() switch
            {
                "ATK" => Character.TotalPhysicalAttack,
                "MATK" => Character.TotalMagicAttack,
                "SPR" => Character.TotalSPR,
                "INT" => Character.TotalINT,
                "DEX" => Character.TotalDEX,
                "AIM" => Character.TotalAim * 2,
                "EVA" => Character.TotalEvasion * 2,
                "END" => Character.TotalEND,
                "STR" => Character.TotalSTR,
                _ => Character.TotalPhysicalAttack
            };

        }

        private void EndPlayerAction()
        {
            if (!Enemy.IsAlive)
            {
                FinishPlayerWon();
                return;
            }

            if (RecoveryTurnsRemaining > 0)
            {
                Phase = CombatPhase.Recovery;
                EnemyTurn();
                return;
            }

            EnemyTurn();
        }

        private void EnemyTurn()
        {
            if (!Enemy.IsAlive) { FinishPlayerWon(); return; }
            if (!Character.IsAlive) { FinishPlayerLost(); return; }

            int dmg = CombatSystem.CalculateDamage(Enemy, Character);
            if (dmg <= 0)
                Log.Add(new CombatLogEntry("pg.fight.log.enemyMiss", Enemy.Name));
            else
            {
                Character.ApplyDamage(dmg);
                Log.Add(new CombatLogEntry("pg.fight.log.enemyHit", Enemy.Name, dmg));
            }

            if (!Character.IsAlive) FinishPlayerLost();
            else Phase = (RecoveryTurnsRemaining > 0) ? CombatPhase.Recovery : CombatPhase.PlayerTurn;
        }
        public Dictionary<string, int> GetDropNames()
        {
            return _dropnumbers;
        }
        private static long ScaleXp(long baseXp, int playerLevel, int monsterLevel)
        {
            if (playerLevel <= monsterLevel) return baseXp;
            double ratio = (double)monsterLevel / playerLevel;
            return Math.Max(1L, (long)(baseXp * ratio * ratio));
        }

        private void FinishPlayerWon()
        {
            Phase = CombatPhase.Finished;
            long xpGained = ScaleXp(Enemy.Exp, Character.Level, Enemy.Level);
            Character.GainXp(xpGained);
            ClassManager.GrantClassXp(Character, xpGained);
            SkillFactory.UpdateSkills(Character);

            MonsterKilled?.Invoke(this, new MonsterKilledEventArgs(Enemy.Id));

            _drops = LootGenerator.GetLootFor(Enemy);
            _dropnumbers = new();
            foreach (var drop in _drops)
            {
                _dropnumbers.Add(drop.Name, drop.StackSize);
            }
            foreach (var drop in _drops)
            {
                if (drop.StackSize == 0)
                    drop.StackSize = 1;
                if (!Character.Inventory.AddItem(drop, Character))
                    InventoryFull = true;
            }
            if (Character.CurrentRoom.IsDungeonRoom)
            {
                Character.CurrentRoom.CurrentMonsters.Remove(Enemy);
                if (Character.CurrentRoom.CurrentMonsters.Count == 0)
                    Character.CurrentRoom.IsCleared = true;
            }

            DayCycleManager.AddTicks(GameTick.CombatVictory);
            Log.Add(new CombatLogEntry("pg.fight.log.win", Enemy.Name));
        }

        private void UpdateQuestProgress(object? sender, MonsterKilledEventArgs e)
        {
            foreach (var quest in Character.ActiveQuests.Where(q => q.Status == QuestStatus.InProgress))
            {
                if (!quest.RequiredKills.TryGetValue(e.MonsterId, out int required))
                    continue;

                if (!quest.KillProgress.ContainsKey(e.MonsterId))
                    quest.KillProgress[e.MonsterId] = 0;

                if (quest.KillProgress[e.MonsterId] >= required)
                    continue;

                quest.KillProgress[e.MonsterId]++;

                if (quest.RequiredKills.All(rk => quest.KillProgress.TryGetValue(rk.Key, out int p) && p >= rk.Value))
                    quest.Status = QuestStatus.Completed;
            }
        }

        private void FinishPlayerLost()
        {
            Phase = CombatPhase.Finished;
            Log.Add(new CombatLogEntry("pg.fight.log.lose"));
        }

    }

}
