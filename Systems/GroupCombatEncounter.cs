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
    public sealed class GroupCombatEncounter
    {
        public IReadOnlyList<Character>  Characters  { get; }
        public List<Monster>          Monsters { get; }

        public bool IsFinished  { get; private set; }
        public bool CharactersWon  { get; private set; }
        public List<CombatLogEntry> Log { get; } = new();

        private int _currentCharacterIndex;

        public string CurrentTurnCharacterName =>
            IsFinished || _currentCharacterIndex >= Characters.Count
                ? ""
                : Characters[_currentCharacterIndex].Name;

        public event EventHandler<MonsterKilledEventArgs>? MonsterKilled;

        public GroupCombatEncounter(IEnumerable<Character> characters, IEnumerable<Monster> enemies)
        {
            var rng = Random.Shared;
            Characters = characters
                .Select(p => (character: p, tieBreak: rng.Next()))
                .OrderByDescending(x => x.character.TotalDEX)
                .ThenBy(x => x.tieBreak)
                .Select(x => x.character)
                .ToList();

            Monsters = enemies.Select(m => { m.ResetHealth(); return m; }).ToList();
            Log.Add(new CombatLogEntry("pg.fight.log.start",
                string.Join(", ", Monsters.Select(m => m.Name))));
            Log.Add(new CombatLogEntry("pg.fight.log.order",
                string.Join(", ", Characters.Select(p => p.Name))));
            SkipDeadCharacters();
        }

        // ── Actions ───────────────────────────────────────────────────────────

        public bool CharacterAttack(string playerName, int targetMonsterIndex)
        {
            if (IsFinished || !IsThisCharactersTurn(playerName)) return false;

            var attacker = Characters[_currentCharacterIndex];
            var target   = GetMonster(targetMonsterIndex);
            if (target == null || !target.IsAlive) return false;

            var (dmg, isCrit) = CombatSystem.CalculateDamageWithCrit(attacker, target);
            if (dmg <= 0)
                Log.Add(new CombatLogEntry("pg.fight.log.miss", attacker.Name));
            else
            {
                target.TakeDamage(dmg);
                Log.Add(isCrit
                    ? new CombatLogEntry("pg.fight.log.critHit", attacker.Name, dmg)
                    : new CombatLogEntry("pg.fight.log.hit", attacker.Name, dmg));
                HandleMonsterDeath(target);
            }

            if (!IsFinished) AdvanceAfterCharacterAction();
            return true;
        }

        public bool CharacterCastSkill(string playerName, Skill skill, int targetIndex)
        {
            if (IsFinished || !IsThisCharactersTurn(playerName)) return false;

            var caster = Characters[_currentCharacterIndex];
            if (caster.CurrentMana < skill.ManaCost)
            {
                Log.Add(new CombatLogEntry("pg.fight.log.nomana"));
                return false;
            }

            caster.SpendMana(skill.ManaCost);

            switch (skill.Target)
            {
                case SkillTarget.Self:
                    ExecuteSkillOnCharacter(skill, caster, caster);
                    break;

                case SkillTarget.SingleAlly:
                    var ally = GetCharacter(targetIndex);
                    if (ally == null || !ally.IsAlive) { caster.SpendMana(-skill.ManaCost); return false; }
                    ExecuteSkillOnCharacter(skill, caster, ally);
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

            if (!IsFinished) AdvanceAfterCharacterAction();
            return true;
        }

        // ── Internal helpers ──────────────────────────────────────────────────

        private void ExecuteSkillOnCharacter(Skill skill, Character caster, Character target)
        {
            if (!skill.IsHealing) return;
            int baseStat = ResolveBaseStat(skill, caster);
            int heal     = (int)(baseStat * skill.ScalingFactor);
            int healed   = Math.Min(heal, target.MaxHealth - target.CurrentHealth);
            target.Heal(healed);
            Log.Add(new CombatLogEntry("pg.fight.log.heal", caster.Name, healed));
        }

        private void ExecuteSkillOnMonster(Skill skill, Character caster, ICombatant target)
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
            foreach (var p in Characters.Where(p => p.IsAlive))
            {
                long xpGained = ScaleXp(monster.Exp, p.Level, monster.Level);
                p.GainXp(xpGained);
                ClassManager.GrantClassXp(p, xpGained);
            }

            // Loot to first living player
            var recipient = Characters.FirstOrDefault(p => p.IsAlive);
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
                FinishCharactersWon();
        }

        private void AdvanceAfterCharacterAction()
        {
            int next = _currentCharacterIndex + 1;
            while (next < Characters.Count && !Characters[next].IsAlive) next++;

            if (next >= Characters.Count)
            {
                // All living players have acted — monsters' turn
                MonstersTurn();
                if (!IsFinished)
                    SkipDeadCharacters(); // restart from first living player
            }
            else
            {
                _currentCharacterIndex = next;
            }
        }

        private void SkipDeadCharacters()
        {
            int idx = 0;
            while (idx < Characters.Count && !Characters[idx].IsAlive) idx++;
            _currentCharacterIndex = idx;
            if (_currentCharacterIndex >= Characters.Count) FinishCharactersLost();
        }

        private void MonstersTurn()
        {
            var living = Characters.Where(p => p.IsAlive).ToList();
            if (living.Count == 0) { FinishCharactersLost(); return; }

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

            if (!Characters.Any(p => p.IsAlive)) FinishCharactersLost();
        }

        private void FinishCharactersWon()
        {
            IsFinished = true;
            CharactersWon = true;
            foreach (var p in Characters)
                SkillFactory.UpdateSkills(p);
        }

        private void FinishCharactersLost()
        {
            IsFinished = true;
            CharactersWon = false;
            Log.Add(new CombatLogEntry("pg.fight.log.lose"));
        }

        private void UpdateQuestProgress(int monsterId)
        {
            foreach (var player in Characters)
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

        private bool IsThisCharactersTurn(string playerName) =>
            _currentCharacterIndex < Characters.Count &&
            string.Equals(Characters[_currentCharacterIndex].Name, playerName, StringComparison.OrdinalIgnoreCase);

        private Monster? GetMonster(int index) =>
            index >= 0 && index < Monsters.Count ? Monsters[index] : null;

        private Character? GetCharacter(int index) =>
            index >= 0 && index < Characters.Count ? Characters[index] : null;

        private static int ResolveBaseStat(Skill skill, Character character) =>
            skill.StatToScaleFrom.ToUpper() switch
            {
                "ATK"  => character.TotalPhysicalAttack,
                "MATK" => character.TotalMagicAttack,
                "SPR"  => character.TotalSPR,
                "INT"  => character.TotalINT,
                "DEX"  => character.TotalDEX,
                "AIM"  => character.TotalAim * 2,
                "EVA"  => character.TotalEvasion * 2,
                "END"  => character.TotalEND,
                "STR"  => character.TotalSTR,
                _      => character.TotalPhysicalAttack
            };
    }
}
