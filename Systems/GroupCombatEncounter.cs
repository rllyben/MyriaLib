using MyriaLib.Entities;
using MyriaLib.Entities.Effects;
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

        /// <summary>
        /// Total actual (level-scaled) XP granted to each character across this encounter so far,
        /// accumulated per kill. Read this instead of summing Monsters' raw Exp values for
        /// anything XP-derived (UI display, bonus calculations) — each character can be scaled
        /// differently per kill depending on their own level vs. that monster's level.
        /// </summary>
        public Dictionary<Character, long> XpGrantedByCharacter { get; } = new();

        /// <summary>Item ids granted to each character across this encounter so far (accumulated
        /// per kill, one entry per dropped item). Only the single living recipient of a given kill's
        /// loot gets entries added for that kill.</summary>
        public Dictionary<Character, List<string>> LootGrantedByCharacter { get; } = new();

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

            // Reset aggro for all participants at encounter start.
            foreach (var c in Characters)
                c.AggroLevel = 0f;

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

            bool attackerStunned = EffectProcessor.IsStunned(attacker);
            foreach (var entry in EffectProcessor.Tick(attacker))
                Log.Add(entry);

            if (!attacker.IsAlive)
            {
                AdvanceAfterCharacterAction();
                return false;
            }

            if (attackerStunned)
            {
                Log.Add(new CombatLogEntry("pg.fight.effect.stunned", attacker.Name));
                AdvanceAfterCharacterAction();
                return true;
            }

            var target = GetMonster(targetMonsterIndex);
            if (target == null || !target.IsAlive) return false;

            attacker.AggroLevel += 1f;

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

            bool casterStunned = EffectProcessor.IsStunned(caster);
            foreach (var entry in EffectProcessor.Tick(caster))
                Log.Add(entry);

            if (!caster.IsAlive)
            {
                AdvanceAfterCharacterAction();
                return false;
            }

            if (casterStunned)
            {
                Log.Add(new CombatLogEntry("pg.fight.effect.stunned", caster.Name));
                AdvanceAfterCharacterAction();
                return true;
            }

            if (EffectProcessor.IsSilenced(caster))
            {
                Log.Add(new CombatLogEntry("pg.fight.effect.silenced", caster.Name));
                return false;
            }

            if (caster.CurrentMana < skill.ManaCost)
            {
                Log.Add(new CombatLogEntry("pg.fight.log.nomana"));
                return false;
            }

            caster.SpendMana(skill.ManaCost);
            caster.AggroLevel += 1f + skill.AggroModifier;

            // Resolve primary effect targets based on skill targeting.
            List<CombatEntity> primaryTargets = skill.Target switch
            {
                SkillTarget.AllEnemies  => Monsters.Where(m => m.IsAlive).Cast<CombatEntity>().ToList(),
                SkillTarget.SingleEnemy => GetMonster(targetIndex) is { IsAlive: true } m
                                              ? new List<CombatEntity> { m }
                                              : new List<CombatEntity>(),
                SkillTarget.SingleAlly  => GetCharacter(targetIndex) is { IsAlive: true } a
                                              ? new List<CombatEntity> { a }
                                              : new List<CombatEntity> { caster },
                SkillTarget.AllAllies   => Characters.Where(c => c.IsAlive).Cast<CombatEntity>().ToList(),
                _                       => new List<CombatEntity> { caster }, // Self
            };

            int totalDamageForLifesteal = 0;

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

                case SkillTarget.AllAllies:
                    foreach (var member in Characters.Where(c => c.IsAlive).ToList())
                        ExecuteSkillOnCharacter(skill, caster, member);
                    break;

                case SkillTarget.SingleEnemy:
                    var enemy = GetMonster(targetIndex);
                    if (enemy == null || !enemy.IsAlive) { caster.SpendMana(-skill.ManaCost); return false; }
                    totalDamageForLifesteal += ExecuteSkillOnMonster(skill, caster, enemy);
                    HandleMonsterDeath(enemy);
                    break;

                case SkillTarget.AllEnemies:
                    foreach (var m in Monsters.Where(m => m.IsAlive).ToList())
                    {
                        totalDamageForLifesteal += ExecuteSkillOnMonster(skill, caster, m);
                        HandleMonsterDeath(m);
                        if (IsFinished) break;
                    }
                    break;

                default:
                    // Unrecognized/mod-added Target: the resolution switch above already treats
                    // this as Self (see primaryTargets), so execution matches that instead of
                    // silently doing nothing (mana already spent) as it would have before.
                    ExecuteSkillOnCharacter(skill, caster, caster);
                    break;
            }

            // Apply data-driven effects to resolved targets.
            int effectStat = ResolveEffectStat(skill, caster);
            foreach (var entry in skill.Effects)
            {
                var hosts = entry.ApplyTo switch
                {
                    EffectTarget.Caster    => new List<CombatEntity> { caster },
                    EffectTarget.AllAllies => Characters.Where(c => c.IsAlive).Cast<CombatEntity>().ToList(),
                    _                      => primaryTargets
                };

                foreach (var host in hosts)
                {
                    var effect = EffectFactory.CreateEffect(entry.EffectId, skill.Id, skill.ScalingFactor, effectStat);
                    if (effect == null) continue;
                    effect.SourceCharacterName = caster.Name;

                    if (effect.Type == EffectType.LifeSteal)
                    {
                        int healAmount = Math.Max(1, (int)(totalDamageForLifesteal * effect.Magnitude));
                        caster.Heal(healAmount);
                        Log.Add(new CombatLogEntry("pg.fight.effect.lifesteal", caster.Name, healAmount));
                    }
                    else if (effect.Type == EffectType.ManaSiphon)
                    {
                        int manaAmount = Math.Max(1, (int)(totalDamageForLifesteal * effect.Magnitude));
                        caster.RestoreMana(manaAmount);
                        Log.Add(new CombatLogEntry("pg.fight.effect.manasiphon", caster.Name, manaAmount));
                    }
                    else
                    {
                        EffectProcessor.Apply(host, effect);
                        Log.Add(new CombatLogEntry("pg.fight.effect.applied", effect.Name, host.Name));
                    }
                }
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

        private int ExecuteSkillOnMonster(Skill skill, Character caster, ICombatant target)
        {
            if (skill.IsHealing) return 0;
            int baseStat = ResolveBaseStat(skill, caster);
            float raw = baseStat * skill.ScalingFactor;
            float def = skill.Type == SkillType.Physical
                ? target.TotalPhysicalDefense
                : target.TotalMagicDefense;
            int dmg = Math.Max(1, (int)(raw * (raw / (raw + def))));
            target.TakeDamage(dmg);
            Log.Add(new CombatLogEntry("pg.fight.log.skillHit", caster.Name, skill.Name, dmg));
            return dmg;
        }

        private void HandleMonsterDeath(Monster monster)
        {
            if (monster.IsAlive) return;

            bool monsterHasAllies = Monsters.Any(m => m.IsAlive && m != monster);
            if (EffectProcessor.TryResurrect(monster, monsterHasAllies))
            {
                Log.Add(new CombatLogEntry("pg.fight.effect.resurrected", monster.Name));
                return;
            }

            Log.Add(new CombatLogEntry("pg.fight.log.win", monster.Name));
            MonsterKilled?.Invoke(this, new MonsterKilledEventArgs(monster.Id));

            // Quest kill-credit goes to every party member, alive or not (matches prior behavior);
            // XP/class-XP is alive-only, handled separately below.
            foreach (var p in Characters)
                GameEvents.FireMonsterKilled(p, monster);

            foreach (var p in Characters.Where(p => p.IsAlive))
            {
                long xpGained = ScaleXp(monster.Exp, p.Level, monster.Level);
                p.GainXp(xpGained);
                ClassManager.GrantClassXp(p, xpGained);
                SkillFactory.UpdateSkills(p);
                XpGrantedByCharacter[p] = XpGrantedByCharacter.GetValueOrDefault(p) + xpGained;
            }

            var recipient = Characters.FirstOrDefault(p => p.IsAlive);
            if (recipient != null)
            {
                var drops = LootGenerator.GetLootFor(monster);
                if (!LootGrantedByCharacter.TryGetValue(recipient, out var lootList))
                    LootGrantedByCharacter[recipient] = lootList = new();
                foreach (var drop in drops)
                {
                    if (drop.StackSize == 0) drop.StackSize = 1;
                    recipient.Inventory.AddItem(drop, recipient);
                    lootList.Add(drop.Id);
                }
            }

            // Mirrors CombatEncounter.FinishCharacterWon's dungeon room-clearing - previously only
            // the solo path did this, so dungeon monsters killed via group combat never left the
            // room and dungeons could never be marked cleared.
            var room = Characters.FirstOrDefault()?.CurrentRoom;
            if (room != null && room.IsDungeonRoom)
            {
                room.CurrentMonsters.Remove(monster);
                if (room.CurrentMonsters.Count == 0)
                    room.IsCleared = true;
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

            foreach (var monster in Monsters.Where(m => m.IsAlive).ToList())
            {
                // Credit 0.5 aggro per Poison DoT tick to the character who applied each effect.
                foreach (var effect in monster.ActiveEffects.Where(e => e.Type == EffectType.Poison && e.TurnsRemaining > 0))
                {
                    var source = living.FirstOrDefault(c => c.Name == effect.SourceCharacterName);
                    if (source != null)
                        source.AggroLevel += 0.5f;
                }

                bool monsterStunned = EffectProcessor.IsStunned(monster);
                foreach (var entry in EffectProcessor.Tick(monster))
                    Log.Add(entry);

                if (!monster.IsAlive) continue;

                if (monsterStunned)
                {
                    Log.Add(new CombatLogEntry("pg.fight.effect.stunned", monster.Name));
                    continue;
                }

                // Target the character with the highest aggro; on ties, first in initiative order.
                var currentLiving = Characters.Where(c => c.IsAlive).ToList();
                if (currentLiving.Count == 0) break;
                var target = currentLiving.OrderByDescending(c => c.AggroLevel).First();

                int dmg = CombatSystem.CalculateDamage(monster, target);
                if (dmg <= 0)
                    Log.Add(new CombatLogEntry("pg.fight.log.enemyMiss", monster.Name));
                else
                {
                    target.ApplyDamage(dmg);
                    Log.Add(new CombatLogEntry("pg.fight.log.enemyHit", monster.Name, dmg));

                    if (!target.IsAlive)
                    {
                        bool hasCharAllies = Characters.Any(c => c.IsAlive && c != target);
                        if (EffectProcessor.TryResurrect(target, hasCharAllies))
                            Log.Add(new CombatLogEntry("pg.fight.effect.resurrected", target.Name));
                    }
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

        // Uses the caster's PRIMARY stat for effect magnitude scaling.
        // ATK/MATK map to their underlying stat (STR/INT) so all skills
        // start at a similar baseline (~8-10 at level 1).
        private static int ResolveEffectStat(Skill skill, Character character) =>
            skill.StatToScaleFrom.ToUpper() switch
            {
                "ATK"                   => character.TotalSTR,
                "MATK"                  => character.TotalINT,
                "SPR"                   => character.TotalSPR,
                "INT"                   => character.TotalINT,
                "DEX" or "AIM" or "EVA" => character.TotalDEX,
                "END"                   => character.TotalEND,
                "STR"                   => character.TotalSTR,
                _                       => character.TotalSTR
            };
    }
}
