using MyriaLib.Entities.Monsters;
using MyriaLib.Entities.Players;
using MyriaLib.Entities.Skills;
using MyriaLib.Services;
using MyriaLib.Services.Builder;
using MyriaLib.Systems.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public CombatEncounter(Player player, Monster enemy)
        {
            Player = player;
            Enemy = enemy;
            Enemy.ResetHealth(); // if you do that elsewhere, remove
            Log.Add(new CombatLogEntry("pg.fight.log.start", enemy.Name));
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
            // This mirrors your existing UseSkill logic :contentReference[oaicite:3]{index=3}
            // but logs keys instead of Console.WriteLine.
            Player.CurrentMana -= skill.ManaCost;

            if (skill.IsHealing)
            {
                int baseStat = ResolveSkillBaseStat(skill);
                int heal = (int)(baseStat * skill.ScalingFactor);
                int healed = Math.Min(heal, Player.Stats.MaxHealth - Player.CurrentHealth);
                Player.CurrentHealth += healed;
                Log.Add(new CombatLogEntry("pg.fight.log.heal", Player.Name, healed));
            }
            else
            {
                int baseStat = ResolveSkillBaseStat(skill);
                int dmg = (int)(baseStat * skill.ScalingFactor);
                Enemy.TakeDamage(dmg);
                Log.Add(new CombatLogEntry("pg.fight.log.skillHit", Player.Name, skill.Name, dmg));
            }

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
                Player.TakeDamage(dmg);
                Log.Add(new CombatLogEntry("pg.fight.log.enemyHit", Enemy.Name, dmg));
            }

            if (!Player.IsAlive) FinishPlayerLost();
            else Phase = (RecoveryTurnsRemaining > 0) ? CombatPhase.Recovery : CombatPhase.PlayerTurn;
        }
        private void FinishPlayerWon()
        {
            Phase = CombatPhase.Finished;
            Player.Experience += Enemy.Exp;
            Player.CheckForLevelup();
            SkillFactory.UpdateSkills(Player);

            foreach (var quest in Player.ActiveQuests.Where(q => q.Status == QuestStatus.InProgress))
            {
                if (quest.RequiredKills.TryGetValue(Enemy.Id, out int required))
                {
                    if (quest.KillProgress.Count < 1)
                        quest.KillProgress.Add(Enemy.Id, 0);
                    else if (quest.KillProgress.All(a => a.Key != Enemy.Id))
                        quest.KillProgress.Add(Enemy.Id, 0);
                    if (quest.KillProgress[Enemy.Id] >= quest.RequiredKills[Enemy.Id])
                        continue;
                    quest.KillProgress[Enemy.Id]++;
                    int current = quest.KillProgress[Enemy.Id];

                    if (quest.KillProgress.All(kp => kp.Value >= quest.RequiredKills[kp.Key]))
                    {
                        quest.Status = QuestStatus.Completed;
                    }

                }

            }

            var drops = LootGenerator.GetLootFor(Enemy);

            if (drops.Count > 0)
            {
                if (Enemy.DropsCorpse)
                {
                    var corpse = new Corpse(Enemy.Name, drops);
                    Player.CurrentRoom.Corpses.Add(corpse);
                }
                else
                {
                    foreach (var drop in drops)
                    {
                        if (Player.Inventory.AddItem(drop, Player))
                        {
                            Console.WriteLine();
                        }
                        else
                        {
                            Console.WriteLine();
                        }
                    }

                }

            }
            if (Player.CurrentRoom.IsDungeonRoom && Player.CurrentRoom.CurrentMonsters.Count < 1)
            {
                Player.CurrentRoom.IsCleared = true;
            }
            Log.Add(new CombatLogEntry("pg.fight.log.win", Enemy.Name));
        }

        private void FinishPlayerLost()
        {
            Phase = CombatPhase.Finished;
            Log.Add(new CombatLogEntry("pg.fight.log.lose"));
        }

    }

}
