using MyriaLib.Entities.Items;
using MyriaLib.Entities.Monsters;
using MyriaLib.Entities.Players;
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
        public Player Player { get; }
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

        public CombatEncounter(Player player, Monster enemy)
        {
            Player = player;
            Enemy = enemy;
            Enemy.ResetHealth(); // if you do that elsewhere, remove
            Log.Add(new CombatLogEntry("pg.fight.log.start", enemy.Name));
            MonsterKilled += UpdateQuestProgress;
        }

        public void PlayerAttack()
        {
            if (Phase != CombatPhase.PlayerTurn) return;

            int dmg = CombatSystem.CalculateDamage(Player, Enemy);
            if (dmg <= 0)
                Log.Add(new CombatLogEntry("pg.fight.log.miss", Player.Name));
            else
            {
                Enemy.TakeDamage(dmg);
                Log.Add(new CombatLogEntry("pg.fight.log.hit", Player.Name, dmg));
            }

            EndPlayerAction();
        }

        public bool PlayerBeginCast(Skill skill)
        {
            if (Phase != CombatPhase.PlayerTurn) return false;
            if (Player.CurrentMana < skill.ManaCost)
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

                Log.Add(new CombatLogEntry("pg.fight.log.beginCast", Player.Name, skill.Name));
            }
            else
            {
                ExecuteSkill(skill);
                RecoveryTurnsRemaining = skill.RecoveryTime;
                EndPlayerAction();
            }

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
            Player.SpendMana(skill.ManaCost);

            switch (skill.Target)
            {
                case SkillTarget.Self:
                    ExecuteSkillOnSelf(skill);
                    break;

                case SkillTarget.AllEnemies:
                    // Current combat is 1v1; hitting the single enemy covers the intent.
                    // When group combat is added, iterate all enemies here instead.
                    ExecuteSkillOnEnemy(skill, Enemy);
                    break;

                case SkillTarget.SingleEnemy:
                default:
                    ExecuteSkillOnEnemy(skill, Enemy);
                    break;
            }

            // Invoke optional code-defined effect last (buffs, status effects, etc.)
            skill.Effect?.Invoke(Player, Enemy);
        }

        private void ExecuteSkillOnSelf(Skill skill)
        {
            if (skill.IsHealing)
            {
                int baseStat = ResolveSkillBaseStat(skill);
                int heal = (int)(baseStat * skill.ScalingFactor);
                int healed = Math.Min(heal, Player.MaxHealth - Player.CurrentHealth);
                Player.Heal(healed);
                Log.Add(new CombatLogEntry("pg.fight.log.heal", Player.Name, healed));
            }
            // Non-healing Self skills (buffs) rely entirely on skill.Effect invoked after this.
        }

        private void ExecuteSkillOnEnemy(Skill skill, ICombatant target)
        {
            if (skill.IsHealing) return; // healing a target-enemy would be a design mistake; skip

            int baseStat = ResolveSkillBaseStat(skill);
            int dmg = (int)(baseStat * skill.ScalingFactor);
            target.TakeDamage(dmg);
            Log.Add(new CombatLogEntry("pg.fight.log.skillHit", Player.Name, skill.Name, dmg));
        }

        private int ResolveSkillBaseStat(Skill skill)
        {
            // same mapping you already have :contentReference[oaicite:4]{index=4}
            return skill.StatToScaleFrom.ToUpper() switch
            {
                "ATK" => Player.TotalPhysicalAttack,
                "MATK" => Player.TotalMagicAttack,
                "SPR" => Player.TotalSPR,
                "INT" => Player.TotalINT,
                "DEX" => Player.TotalDEX,
                "AIM" => Player.TotalAim * 2,
                "EVA" => Player.TotalEvasion * 2,
                "END" => Player.TotalEND,
                "STR" => Player.TotalSTR,
                _ => Player.TotalPhysicalAttack
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
            if (!Player.IsAlive) { FinishPlayerLost(); return; }

            int dmg = CombatSystem.CalculateDamage(Enemy, Player);
            if (dmg <= 0)
                Log.Add(new CombatLogEntry("pg.fight.log.enemyMiss", Enemy.Name));
            else
            {
                Player.ApplyDamage(dmg);
                Log.Add(new CombatLogEntry("pg.fight.log.enemyHit", Enemy.Name, dmg));
            }

            if (!Player.IsAlive) FinishPlayerLost();
            else Phase = (RecoveryTurnsRemaining > 0) ? CombatPhase.Recovery : CombatPhase.PlayerTurn;
        }
        public Dictionary<string, int> GetDropNames()
        {
            return _dropnumbers;
        }
        private void FinishPlayerWon()
        {
            Phase = CombatPhase.Finished;
            Player.GainXp(Enemy.Exp);
            SkillFactory.UpdateSkills(Player);

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
                if (!Player.Inventory.AddItem(drop, Player))
                    InventoryFull = true;
            }
            if (Player.CurrentRoom.IsDungeonRoom)
            {
                Player.CurrentRoom.CurrentMonsters.Remove(Enemy);
                if (Player.CurrentRoom.CurrentMonsters.Count == 0)
                    Player.CurrentRoom.IsCleared = true;
            }

            DayCycleManager.AddTicks(GameTick.CombatVictory);
            Log.Add(new CombatLogEntry("pg.fight.log.win", Enemy.Name));
        }

        private void UpdateQuestProgress(object? sender, MonsterKilledEventArgs e)
        {
            foreach (var quest in Player.ActiveQuests.Where(q => q.Status == QuestStatus.InProgress))
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
