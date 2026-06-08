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
    public sealed class GroupCombatEncounter
    {
        public IReadOnlyList<Player>  Players  { get; }
        public List<Monster>          Monsters { get; }

        public bool IsFinished  { get; private set; }
        public bool PlayersWon  { get; private set; }
        public List<CombatLogEntry> Log { get; } = new();

        private int _currentPlayerIndex;

        public string CurrentTurnPlayerName =>
            IsFinished || _currentPlayerIndex >= Players.Count
                ? ""
                : Players[_currentPlayerIndex].Name;

        public event EventHandler<MonsterKilledEventArgs>? MonsterKilled;

        public GroupCombatEncounter(IEnumerable<Player> players, IEnumerable<Monster> enemies)
        {
            var rng = Random.Shared;
            Players = players
                .Select(p => (player: p, tieBreak: rng.Next()))
                .OrderByDescending(x => x.player.TotalDEX)
                .ThenBy(x => x.tieBreak)
                .Select(x => x.player)
                .ToList();

            Monsters = enemies.Select(m => { m.ResetHealth(); return m; }).ToList();
            Log.Add(new CombatLogEntry("pg.fight.log.start",
                string.Join(", ", Monsters.Select(m => m.Name))));
            Log.Add(new CombatLogEntry("pg.fight.log.order",
                string.Join(", ", Players.Select(p => p.Name))));
            SkipDeadPlayers();
        }

        // ── Actions ───────────────────────────────────────────────────────────

        public bool PlayerAttack(string playerName, int targetMonsterIndex)
        {
            if (IsFinished || !IsThisPlayersTurn(playerName)) return false;

            var attacker = Players[_currentPlayerIndex];
            var target   = GetMonster(targetMonsterIndex);
            if (target == null || !target.IsAlive) return false;

            int dmg = CombatSystem.CalculateDamage(attacker, target);
            if (dmg <= 0)
                Log.Add(new CombatLogEntry("pg.fight.log.miss", attacker.Name));
            else
            {
                target.TakeDamage(dmg);
                Log.Add(new CombatLogEntry("pg.fight.log.hit", attacker.Name, dmg));
                HandleMonsterDeath(target);
            }

            if (!IsFinished) AdvanceAfterPlayerAction();
            return true;
        }

        public bool PlayerCastSkill(string playerName, Skill skill, int targetIndex)
        {
            if (IsFinished || !IsThisPlayersTurn(playerName)) return false;

            var caster = Players[_currentPlayerIndex];
            if (caster.CurrentMana < skill.ManaCost)
            {
                Log.Add(new CombatLogEntry("pg.fight.log.nomana"));
                return false;
            }

            caster.SpendMana(skill.ManaCost);

            switch (skill.Target)
            {
                case SkillTarget.Self:
                    ExecuteSkillOnPlayer(skill, caster, caster);
                    break;

                case SkillTarget.SingleAlly:
                    var ally = GetPlayer(targetIndex);
                    if (ally == null || !ally.IsAlive) { caster.SpendMana(-skill.ManaCost); return false; }
                    ExecuteSkillOnPlayer(skill, caster, ally);
                    break;

                case SkillTarget.SingleEnemy:
                    var enemy = GetMonster(targetIndex);
                    if (enemy == null || !enemy.IsAlive) { caster.SpendMana(-skill.ManaCost); return false; }
                    ExecuteSkillOnMonster(skill, caster, enemy);
                    HandleMonsterDeath(enemy);
                    break;

                case SkillTarget.AllEnemies:
                    foreach (var m in Monsters.Where(m => m.IsAlive).ToList())
                    {
                        ExecuteSkillOnMonster(skill, caster, m);
                        HandleMonsterDeath(m);
                        if (IsFinished) break;
                    }
                    break;
            }

            skill.Effect?.Invoke(caster, GetMonster(targetIndex) ?? (ICombatant)caster);

            if (!IsFinished) AdvanceAfterPlayerAction();
            return true;
        }

        // ── Internal helpers ──────────────────────────────────────────────────

        private void ExecuteSkillOnPlayer(Skill skill, Player caster, Player target)
        {
            if (!skill.IsHealing) return;
            int baseStat = ResolveBaseStat(skill, caster);
            int heal     = (int)(baseStat * skill.ScalingFactor);
            int healed   = Math.Min(heal, target.MaxHealth - target.CurrentHealth);
            target.Heal(healed);
            Log.Add(new CombatLogEntry("pg.fight.log.heal", caster.Name, healed));
        }

        private void ExecuteSkillOnMonster(Skill skill, Player caster, ICombatant target)
        {
            if (skill.IsHealing) return;
            int baseStat = ResolveBaseStat(skill, caster);
            float raw = baseStat * skill.ScalingFactor;
            float def = skill.Type == SkillType.Physical
                ? target.TotalPhysicalDefense
                : target.TotalMagicDefense;
            int dmg = Math.Max(1, (int)(raw * (raw / (raw + def))));
            target.TakeDamage(dmg);
            Log.Add(new CombatLogEntry("pg.fight.log.skillHit", caster.Name, skill.Name, dmg));
        }

        private void HandleMonsterDeath(Monster monster)
        {
            if (monster.IsAlive) return;

            Log.Add(new CombatLogEntry("pg.fight.log.win", monster.Name));
            MonsterKilled?.Invoke(this, new MonsterKilledEventArgs(monster.Id));
            UpdateQuestProgress(monster.Id);

            // XP to all living players, scaled down if the monster is below the player's level
            foreach (var p in Players.Where(p => p.IsAlive))
            {
                long xpGained = ScaleXp(monster.Exp, p.Level, monster.Level);
                p.GainXp(xpGained);
                ClassManager.GrantClassXp(p, xpGained);
            }

            // Loot to first living player
            var recipient = Players.FirstOrDefault(p => p.IsAlive);
            if (recipient != null)
            {
                var drops = LootGenerator.GetLootFor(monster);
                foreach (var drop in drops)
                {
                    if (drop.StackSize == 0) drop.StackSize = 1;
                    recipient.Inventory.AddItem(drop, recipient);
                }
            }

            if (Monsters.All(m => !m.IsAlive))
                FinishPlayersWon();
        }

        private void AdvanceAfterPlayerAction()
        {
            int next = _currentPlayerIndex + 1;
            while (next < Players.Count && !Players[next].IsAlive) next++;

            if (next >= Players.Count)
            {
                // All living players have acted — monsters' turn
                MonstersTurn();
                if (!IsFinished)
                    SkipDeadPlayers(); // restart from first living player
            }
            else
            {
                _currentPlayerIndex = next;
            }
        }

        private void SkipDeadPlayers()
        {
            int idx = 0;
            while (idx < Players.Count && !Players[idx].IsAlive) idx++;
            _currentPlayerIndex = idx;
            if (_currentPlayerIndex >= Players.Count) FinishPlayersLost();
        }

        private void MonstersTurn()
        {
            var living = Players.Where(p => p.IsAlive).ToList();
            if (living.Count == 0) { FinishPlayersLost(); return; }

            var rng = Random.Shared;
            foreach (var monster in Monsters.Where(m => m.IsAlive).ToList())
            {
                var target = living[rng.Next(living.Count)];
                int dmg    = CombatSystem.CalculateDamage(monster, target);
                if (dmg <= 0)
                    Log.Add(new CombatLogEntry("pg.fight.log.enemyMiss", monster.Name));
                else
                {
                    target.ApplyDamage(dmg);
                    Log.Add(new CombatLogEntry("pg.fight.log.enemyHit", monster.Name, dmg));
                }
            }

            if (!Players.Any(p => p.IsAlive)) FinishPlayersLost();
        }

        private void FinishPlayersWon()
        {
            IsFinished = true;
            PlayersWon = true;
            foreach (var p in Players)
                SkillFactory.UpdateSkills(p);
        }

        private void FinishPlayersLost()
        {
            IsFinished = true;
            PlayersWon = false;
            Log.Add(new CombatLogEntry("pg.fight.log.lose"));
        }

        private void UpdateQuestProgress(int monsterId)
        {
            foreach (var player in Players)
            {
                foreach (var quest in player.ActiveQuests.Where(q => q.Status == QuestStatus.InProgress))
                {
                    if (!quest.RequiredKills.TryGetValue(monsterId, out int required)) continue;
                    if (!quest.KillProgress.ContainsKey(monsterId))
                        quest.KillProgress[monsterId] = 0;
                    if (quest.KillProgress[monsterId] >= required) continue;
                    quest.KillProgress[monsterId]++;
                    if (quest.RequiredKills.All(rk =>
                            quest.KillProgress.TryGetValue(rk.Key, out int p) && p >= rk.Value))
                        quest.Status = QuestStatus.Completed;
                }
            }
        }

        private static long ScaleXp(long baseXp, int playerLevel, int monsterLevel)
        {
            if (playerLevel <= monsterLevel) return baseXp;
            double ratio = (double)monsterLevel / playerLevel;
            return Math.Max(1L, (long)(baseXp * ratio * ratio));
        }

        private bool IsThisPlayersTurn(string playerName) =>
            _currentPlayerIndex < Players.Count &&
            string.Equals(Players[_currentPlayerIndex].Name, playerName, StringComparison.OrdinalIgnoreCase);

        private Monster? GetMonster(int index) =>
            index >= 0 && index < Monsters.Count ? Monsters[index] : null;

        private Player? GetPlayer(int index) =>
            index >= 0 && index < Players.Count ? Players[index] : null;

        private static int ResolveBaseStat(Skill skill, Player player) =>
            skill.StatToScaleFrom.ToUpper() switch
            {
                "ATK"  => player.TotalPhysicalAttack,
                "MATK" => player.TotalMagicAttack,
                "SPR"  => player.TotalSPR,
                "INT"  => player.TotalINT,
                "DEX"  => player.TotalDEX,
                "AIM"  => player.TotalAim * 2,
                "EVA"  => player.TotalEvasion * 2,
                "END"  => player.TotalEND,
                "STR"  => player.TotalSTR,
                _      => player.TotalPhysicalAttack
            };
    }
}
