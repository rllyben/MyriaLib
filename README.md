# MyriaLib

MyriaLib is a data-driven, UI-agnostic C# game logic library for turn-based RPGs. It owns every rule that decides what happens in your game — combat, XP curves, inventory, quests, jobs, crafting, world navigation, rune magic — while staying completely ignorant of how (or whether) any of it gets drawn on screen.

It was extracted from Myria, a WPF desktop RPG with an ASP.NET Core multiplayer server, where the exact same MyriaLib code runs unmodified in both the single-player client process and the authoritative server process. That split is the core design idea: **write your rules once, run them anywhere** — a WPF app, a Unity/MonoGame client, a console prototype, or a server that needs to be the final authority over what "really" happened in a fight.

MyriaLib is [MIT licensed](LICENSE) — use it freely in your own projects, commercial or not.

This document is a practical guide for using MyriaLib in your own project. If you're looking for the thesis-oriented architecture writeup for the original Myria project, see `DocMyriaLib.odt` in the repository root instead — this file is aimed at you, a developer who wants to build a different game on top of this library.

---

## Table of Contents

1. [Is this for you?](#1-is-this-for-you)
2. [Installation](#2-installation)
3. [Quick Start](#3-quick-start)
4. [Initialization & Load Order](#4-initialization--load-order)
5. [Configuration (GameConfig)](#5-configuration-gameconfig)
6. [The Content Model — JSON Data Files](#6-the-content-model--json-data-files)
7. [Character & Progression](#7-character--progression)
8. [Inventory & Economy](#8-inventory--economy)
9. [Combat](#9-combat)
10. [Skills — Three Tiers](#10-skills--three-tiers)
11. [Jobs](#11-jobs)
12. [Quests](#12-quests)
13. [World — Rooms, NPCs, Maps](#13-world--rooms-npcs-maps)
14. [Time & Ticks](#14-time--ticks)
15. [Gathering & Crafting](#15-gathering--crafting)
16. [Rune Magic](#16-rune-magic)
17. [Accounts & Persistence](#17-accounts--persistence)
18. [Events Reference](#18-events-reference)
19. [Mod System](#19-mod-system)
20. [Building a Multiplayer Server (IGameDataSource)](#20-building-a-multiplayer-server-igamedatasource)
21. [Known Limitations](#21-known-limitations)
22. [License](#license)

---

## 1. Is this for you?

MyriaLib is a good fit if you're building:

- A **turn-based or tick-based RPG** — combat is round-based (attack / cast / use item, then the enemy replies), not real-time.
- Something **text-based, hybrid, or fully graphical** — the library has no rendering code at all, so it's equally usable from a console app, a WPF/Avalonia desktop client, a Unity or MonoGame game, or a headless server.
- A game where you'd rather **author content in JSON than recompile** — items, monsters, rooms, quests, skills, jobs, races, classes, loot tables, and rune words are all data files, not code.
- Something that might eventually need **authoritative multiplayer** — the same rules that run locally in a single-player process can run inside a server process that multiple clients talk to, with no duplicated logic.

It's *not* a fit if you need real-time/physics-driven combat, or if you want a fully code-first content pipeline (everything here assumes JSON authoring, with the mod system built on the same assumption).

MyriaLib targets **.NET 8**.

---

## 2. Installation

MyriaLib isn't published as a NuGet package yet — reference the project directly:

```xml
<ItemGroup>
  <ProjectReference Include="..\MyriaLib\MyriaLib.csproj" />
</ItemGroup>
```

You also need a `Data/` folder next to your executable containing the JSON content files described in [§6](#6-the-content-model--json-data-files). The easiest way to get a working set is to copy `MyriaLib/Data/common/` from this repository as a starting point and edit from there — every ID, stat, and relationship in those files is just data, so replacing the content replaces the game.

---

## 3. Quick Start

```csharp
using MyriaLib.Entities;
using MyriaLib.Entities.Characters;
using MyriaLib.Services;
using MyriaLib.Services.Builder;
using MyriaLib.Services.Manager;
using MyriaLib.Systems;
using MyriaLib.Systems.Mods;

// 1. Load mods (optional — silently does nothing if the folder doesn't exist)
ModLoader.Load("Mods");

// 2. Tune any values you want to change before content loads (all optional, see §5)
GameConfig.SetClassProgression(maxLevel: 50, xpCostBase: 5_000);

// 3. Load every content file — items, monsters, rooms, quests, skills, jobs, runes...
GameService.InitializeGame();

// 4. Create a character. Base stats normally come from the chosen race's profile.
var raceProfile = RaceProfile.All["Myralu"];
var stats = new Stats
{
    Strength     = 10 + raceProfile.BaseStatBonus["STR"],
    Dexterity    = 10 + raceProfile.BaseStatBonus["DEX"],
    Endurance    = 10 + raceProfile.BaseStatBonus["END"],
    Intelligence = 10 + raceProfile.BaseStatBonus["INT"],
    Spirit       = 10 + raceProfile.BaseStatBonus["SPR"],
    BaseHealth   = 30 + raceProfile.BaseHpBonus,
    BaseMana     = 30 + raceProfile.BaseManaBonus,
};
var character = new Character("Aria", stats) { Race = "Myralu", RaceSelected = true, Class = "Fighter" };

StartingEquipmentService.GrantStartingEquipment(character);
SkillFactory.UpdateSkills(character);              // grants level/class-appropriate skills
character.CurrentRoom = RoomService.GetRoomById(1);
character.CurrentRoomId = character.CurrentRoom.Id;

// 5. Tell the library the session is live (starts per-session bookkeeping — see §4)
GameService.StartSession(character);

// 6. Fight something
var monster = MonsterService.GetMonsterById(1)!.Clone();
var fight = new MyriaLib.Systems.CombatEncounter(character, monster);
fight.CharacterAttack();
while (fight.Phase != MyriaLib.Systems.Enums.CombatPhase.Finished)
    fight.Tick();

foreach (var line in fight.Log)
    Console.WriteLine(line);   // localization key + args — format however your UI needs

// 7. Persist the character
var account = new MyriaLib.Models.UserAccount { Username = "player1" };
CharacterService.SaveCharacter(account, character);
```

A few things worth calling out immediately:

- **Everything is a static service.** There is no `IServiceProvider`/DI container anywhere in MyriaLib — `GameService`, `RoomService`, `SkillFactory`, `JobManager`, and friends are all static classes holding process-wide state. This is deliberate (see [§21](#21-known-limitations) for the implications) and keeps the library trivial to call from anywhere without wiring up a container.
- **Nothing here is async.** Loading is synchronous file I/O; combat and other actions are synchronous method calls. If you're building a server, wrap calls at your API boundary as needed — MyriaLib itself won't get in the way.
- **`GameService.InitializeGame()` loads world content once, process-wide** — not per character. Call it once at startup (or once per hot-reload after a mod change), then create/load as many characters as you want against that loaded world.

---

## 4. Initialization & Load Order

`GameService.InitializeGame(IProgress<string>? progress = null, bool skipGameState = false, IGameDataSource? source = null)` runs every loader in this exact order (later steps depend on earlier ones — e.g. rooms link up monster and NPC references, so monsters and NPCs must already be loaded):

1. `GameStatusService.Load()` — persisted day/time state (skipped if `skipGameState: true`)
2. `RaceProfile.Load()`, `ClassProfile.Load()`, `LootGenerator.Load()`
3. `ItemFactory.LoadItems()`
4. `MonsterService.LoadMonsters()`
5. `NpcService.LoadNpcs()`
6. `RoomService.LoadRooms()` — then rooms are cross-linked to their monsters and NPCs
7. `DayCycleManager.Initialize()` (skipped if `skipGameState: true`)
8. `CraftingService.LoadRecipes()`
9. `JobManager.LoadJobs()`
10. `QuestManager.LoadQuests()`
11. `SkillFactory.LoadSkills()`, `EffectFactory.LoadEffects()`
12. Map registries: `DungeonRegistry`, `CaveRegistry`, `CityRegistry`, `ForestRegistry`
13. `RuneWordService.Load()`, `BaseRuneService.Load()`, `BaseSkillLoader.Load()`, `FusionRecipeService.Load()`, `SkillCombinationService.Load()`, `StartingEquipmentService.Load()`

Call `ModLoader.Load(...)` **before** `InitializeGame()` — every loader above resolves its file path through `ModLoader.ResolvePath()`, so mods need to be registered first for overrides to take effect.

Once a character is ready to play, call `GameService.StartSession(character)`. This is separate from `InitializeGame()` on purpose: `InitializeGame` is a one-time, world-level "server start" step, while `StartSession` is a per-character/per-connection step that fires the `SessionStarted` event (both the instance-style `GameService.SessionStarted` and the mod-facing `GameEvents.SessionStarted` — see [§18](#18-events-reference)) so your app can do things like start an inactivity timer or push initial UI state.

**Hot-reloading content** (e.g. after a mod is toggled) — call `GameService.InitializeGame(null, skipGameState: true)` again. This reloads every content file without touching the persisted day/time state or restarting the day-cycle system.

**Enum display registries** (`equipment_slots.json`, `item_rarities.json`, `time_segments.json`, `gathering_types.json` — display names and sort order for UI dropdowns) are *not* loaded automatically, since not every host needs them. Call the relevant `GameConfig.Load...()` method yourself if you want them — see the caveat in [§21](#21-known-limitations), though: these four files aren't included in the sample `Data/` folder, so you'll need to author them yourself if you use this feature.

---

## 5. Configuration (`GameConfig`)

`MyriaLib.Systems.GameConfig` centralizes every tunable numeric/behavioral value so balancing changes don't require hunting through multiple classes. Call these once at startup, before `InitializeGame()`. Every value has a sensible default — you only need to call the setter if you want something different.

| Area | Method | Purpose |
|---|---|---|
| Currency | `SetCurrencyRatios(silver, gold, platinum, crystal)` | Conversion rates between the five currency tiers (bronze is always 1) |
| Inventory | `SetInventoryPageSize(int)` | Grid page size (default 49 = 7×7) |
| Class progression | `SetClassProgression(maxLevel, xpCostBase)` | Level cap and per-level XP cost base |
| Job progression | `SetJobProgression(...)` | Job level cap, XP cost base, max Fame/Skill multipliers |
| Gathering bonus | `SetGatherBonusThresholds((int Level, int Bonus)[])` | Knowledge-level thresholds for bonus daily gather attempts |
| Equipment upgrades | `SetUpgradeGates(defaultMax, (int Level, int MaxUpgrade)[])` | Knowledge-level thresholds for max upgrade tier |
| Cooldowns | `SetClassCooldown(TimeSpan)`, `SetJobCooldown(TimeSpan)` | Lockout period after switching class/job |
| Inactivity penalty | `SetClassPenaltyPerDay(long)` | Daily XP loss for an inactive class |
| Job daily mechanics | `SetJobDailyMechanics(...)` | Fame gain rate, skill decay cap, fame decay divisor, active-job bonus fraction |
| Action costs | `GameTick.RoomTraversal`, `.NpcInteraction`, `.CombatVictory`, `.Gather`, `.Rest` | Tick cost of individual player actions |
| Enum display data | `GameConfig.LoadEquipmentSlots(path)`, `.LoadItemRarities(path)`, `.LoadTimeSegments(path)`, `.LoadGatheringTypes(path)` | Optional, not auto-loaded — see [§21](#21-known-limitations) |

---

## 6. The Content Model — JSON Data Files

Nearly all game content lives in `Data/common/*.json`, loaded once at startup. This buys you two things: you can rebalance or add content without recompiling, and the [mod system](#19-mod-system) works "for free" — mods just point the loader at a different file.

Deserialization is case-insensitive; the samples below use the casing actually written in this repo's JSON files (camelCase).

| File | Contains | Loaded by |
|---|---|---|
| `items.json` | Equipment, consumables, materials, inventory expansions | `ItemFactory.LoadItems()` |
| `monsters.json` | Monster templates with stats and loot ranges | `MonsterService.LoadMonsters()` |
| `rooms.json` | Rooms, exits, gathering spots, access requirements | `RoomService.LoadRooms()` |
| `npcs.json` | NPCs and the services they offer (heal, trade, upgrade, craft) | `NpcService.LoadNpcs()` |
| `jobs.json` | Job definitions (gathering/crafting) | `JobManager.LoadJobs()` |
| `quests.json` | Quests — objectives, gating, dialog, rewards | `QuestManager.LoadQuests()` |
| `skills.json` | Class-bound regular skills | `SkillFactory.LoadSkills()` |
| `effects.json` | Status-effect definitions (poison, buffs, etc.) used by skills/items | `EffectFactory.LoadEffects()` |
| `base_skills.json`\* | Fusion-skill components | `BaseSkillLoader.Load()` |
| `base_runes.json` | Base runes for the runic-magic system | `BaseRuneService.Load()` |
| `rune_words.json` / `rune_families.json` / `rune_word_pairs.json` | Runic vocabulary, families, explicit pair relationships | `RuneWordService.Load()` |
| `skill_combinations.json` | Named overrides for combined skills | `SkillCombinationService.Load()` |
| `fusion_recipes.json`\* | Named overrides for fusion skills | `FusionRecipeService.Load()` |
| `recipes.json` | Crafting recipes, keyed by NPC | `CraftingService.LoadRecipes()` |
| `starting_items.json` | Starting equipment per class | `StartingEquipmentService.Load()` |
| `races.json` | Playable races — stat bonuses and growth | `RaceProfile.Load()` |
| `classes.json` | Playable classes — stat growth | `ClassProfile.Load()` |
| `loot_tables.json` | Type-based loot tables per monster type | `LootGenerator.Load()` |
| `caves.json` / `cities.json` / `dungeons.json` / `forests.json` | Map-area registries | `CaveRegistry` / `CityRegistry` / `DungeonRegistry` / `ForestRegistry` |
| `shops.json` | Shop buy/sell multipliers and stock lists | — (read directly by host app; not part of `InitializeGame`) |
| `equipment_slots.json` / `item_rarities.json` / `time_segments.json` / `gathering_types.json`\* | Display names/sort order for enum values | Individual `GameConfig.Load...()` calls (opt-in) |

\* **Not included in this repository's sample `Data/` folder.** `base_skills.json` and `fusion_recipes.json` are loaded unconditionally by `InitializeGame()`, but the loaders fail soft (log a warning, load an empty set) rather than throwing — see [§21](#21-known-limitations) before relying on the fusion-skill system.

### Field reference for the most-used files

**`items.json`** (→ `EquipmentItem` / `ConsumableItem` / `MaterialItem` / `InventoryExpansion`, picked by `type`)

| Field | Notes |
|---|---|
| `id`, `name`, `description` | |
| `type` | `"equipment"`, `"consumable"`, `"material"`, or `"inventory_expansion"` |
| `rarity` | Free-form string, default `"Common"` |
| `buyPrice`, `stackSize`, `maxStackSize` | |
| `toolType` | Gathering tool type for materials (`Ore`, `Tree`, `Herb`, …) |
| `allowedClasses` | Equipment-only: which classes may equip it |
| `upgradeCategory` | Equipment-only: which upgrade material category applies |
| `slotType` | Equipment-only: `Weapon` / `Armor` / `Accessory` |
| `baseBonusHP/MP/STR/DEX/END/INT/SPR/ATK/DEF/MATK/MDEF/Aim/Evasion/Crit/Block` | Equipment-only stat bonuses |
| `healAmount`, `manaRestore`, `useEffect` | Consumable-only |

**`quests.json`** (→ `Quest`)

| Field | Notes |
|---|---|
| `id`, `name`, `description`, `giverNpcId`, `returnNpcId` | `returnNpcId` defaults to `giverNpcId` if omitted |
| `requiredLevel`, `requiredClass`, `requiredRace` | Optional base gates |
| `requiredActiveJobId` | Must have this job active |
| `requiredAspectJobId` + `requiredSkillLevel` / `requiredKnowledgeLevel` / `requiredFameLevel` | Job-aspect gate |
| `requiresParty`, `requiredPartySize` | Party gate (`0` = any size, just must be in one) |
| `prerequisiteQuestIds` | Must already be completed |
| `requiredKills`, `requiredItems` | `{ id: amount }` maps — omit both for a talk-only quest |
| `acceptItems` | Granted immediately on accept |
| `rewardXp`, `rewardGold`, `rewardItems` | |
| `jobKnowledgeRewardJobId` / `Amount`, `jobFameRewardJobId` / `Amount` | Optional job-aspect rewards on turn-in |
| `acceptDialog`, `returnDialog` | Lists of `{ speaker, text }` lines |
| `isRepeatable`, `repeatMaxLevel`, `repeatDailyLimit`, `repeatTotalLimit` | `0` = unlimited |

**`skills.json`** (→ `SkillData`)

`id`, `name`, `description`, `class`, `manaCost`, `type` (`Physical`/`Magical`), `target` (`SingleEnemy`/`AllEnemies`/`Self`/`SingleAlly`), `scalingFactor`, `statToScaleFrom`, `minLevel`, `isHealing`, `aggroModifier`, `effects` (data-driven status effects the skill applies).

**`rooms.json`** (→ `Room`)

`id`, `name`, `description`, `isCity`/`isCaveRoom`/`isDungeonRoom`/`isBossRoom`, `exits` (direction → room id), `npcs`, `gatheringSpots`, `requirementType` (`None`/`Level`/`Quest`/`Party`), `accessLevel`, `requiredQuestId`, `encounterableMonsters` (monster id → spawn weight).

**`races.json`** / **`classes.json`** — `race`/`class` name, `baseStatBonus` (races only), `statGrowth`, `baseHpBonus`/`baseManaBonus`/`hpPerLevel`/`manaPerLevel`, `forbiddenClasses` (races only), `group` (classes only — controls XP-transfer bonus on same-group class switches).

For the remaining files (NPCs, jobs, runes, combinations/fusion overrides, crafting recipes, loot tables), the shapes are small and mirror their model classes 1:1 (`MyriaLib.Models*`) — the fastest way to get the exact contract is to open the corresponding class, since every field maps directly with camelCase JSON keys.

---

## 7. Character & Progression

`Character` (in `MyriaLib.Entities.Characters`) extends `CombatEntity`, the shared base for anything that fights (`Character` and `Monster` both derive from it). `CombatEntity` computes all the "total" stats you actually use in combat — `TotalSTR`, `MaxHealth`, `TotalPhysicalAttack`, `CritChance`, etc. — by layering race/class base stats, player-invested stat points, equipped-gear bonuses, and active status effects. You should almost never read `Stats.Strength` directly; read `character.TotalSTR` instead so gear and buffs are included.

```csharp
character.GainXp(150);           // levels up automatically in a loop if enough XP was granted at once
character.Equip(sword);          // equips into the correct slot based on EquipmentItem.SlotType
character.LearnSkill(fireball);  // no-ops if already known
bool canGather = character.HasToolFor(GatheringType.Ore);
```

Class progression runs on a separate curve from character level (`ClassManager.GrantClassXp`), lets a player switch class every `ClassCooldown` (default 7 days), and refunds 50% of accumulated class XP when switching within the same `Group` (e.g. `Physical` → `Physical`) to avoid punishing a same-playstyle swap as hard as a full reinvention. `ClassManager.ApplyDailyPenalty` should be wired to your day-advance handling (see [§14](#14-time--ticks)) to apply the inactivity XP decay.

---

## 8. Inventory & Economy

`Inventory` manages items page-by-page (`PageSize`, default 49 = 7×7) with `AddItem`, `RemoveItem`, `UseItem`, `SwapEquipment`, and `SellItem`. Subscribe to `ItemReceived` / `ItemRemoved` / `ItemSold` instead of polling — quest kill/collect progress is wired through these same events internally, so you get that behavior for free.

Currency uses the immutable `Money` value type stored in the smallest denomination (bronze) to avoid rounding errors across the five tiers (bronze/silver/gold/platinum/crystal). `MoneyBag` wraps a character's balance with `CanAfford`, `TryAdd`, and `TrySpend`. `MoneyFormatter` renders it in several styles (`"S"` short, `"L"` long, `"C"` compact, `"B"` raw) and parses user-typed amounts back.

---

## 9. Combat

A one-on-one fight is a `CombatEncounter(character, monster)`. Each of your turn's actions is one call — `CharacterAttack()`, `CharacterBeginCast(skill)`, or `CharacterUseItem(consumable)` — and if the chosen skill has a cast or recovery time, you keep calling `Tick()` to advance until the pending action resolves. `Phase` (`CharacterTurn` / `Casting` / `Recovery` / `Finished`) and the localized `Log` (a list of `CombatLogEntry` — a localization key plus format args, so you render it however your UI needs) are readable at any time. On victory, loot is added to inventory automatically; if there's no room, `InventoryFull` is set instead of silently dropping items. `MonsterKilled` fires for both quest-progress tracking (handled internally) and anything else you want to hook.

`GroupCombatEncounter` extends the same idea to multiple characters vs. multiple monsters with round-robin turns; loot goes to the first living character, and quest progress updates for every participant. Both combat types share the same underlying `CombatSystem` (`TryHit`, `CalculateDamage`) if you just need the raw formulas — e.g. to show a damage-range tooltip without starting a real fight.

---

## 10. Skills — Three Tiers

| Tier | Class | How it's created |
|---|---|---|
| Regular | `Skill` | Auto-granted on level-up/class-change via `SkillFactory.UpdateSkills()` |
| Combined | `CombinedSkill` | Player combines 2–5 learned regular skills of the same class via `SkillCombinationService.TryCreateForCharacter(character, skillIds)` |
| Fusion | `CompositeSkill` | Player fuses base-skill components (from `base_skills.json`) via `SkillFusionSystem.TryCreateForCharacter(character, components)` |

Combined skills apply a balance rule: AoE combinations get a 10% scaling penalty, single-target combinations get a 10% bonus, so combining doesn't make AoE skills disproportionately strong. `SkillSlotService` manages the combat skill bar across all three tiers — `TryAddSlot`, `RemoveSlot`, `ReorderSlots`, `GetCombatSkills(character)` — with slot count scaling by level (`SkillSlotCount`) and a separately-capped fusion slot count (`FusionSlotCount`) so fusion skills stay a deliberately scarce resource.

> **Before you build on fusion skills:** the *display and combat-use* code (`SkillSlotService`, skill-bar UI hooks) fully supports `CompositeSkill`, but nothing in this library calls `SkillFusionSystem.TryCreateForCharacter` for you — you need to build your own "combine these components" UI/API that calls it, the same way you'd build one for `SkillCombinationService`. See [§21](#21-known-limitations).

---

## 11. Jobs

Jobs are independent of class — a character can hold several jobs over time but only one *active* job at once. Each job tracks three independent progress axes via `CharacterJob`:

| Axis | Gained from | Lost from |
|---|---|---|
| Skill XP | Gathering/crafting (+50% while the job is active) | Daily decay if unused (up to 50 XP/day) |
| Knowledge XP | Job-master quests | Resets to the current level floor each day (levels already reached are kept) |
| Fame XP | Activity in the active job + 5 XP/day passively | ~0.5%/day decay while the job is inactive |

`JobManager.SetActiveJob`, `.CanChangeJob`, and `.GetCooldownRemaining` gate job switching the same way class switching is gated (default 7-day cooldown). Call `JobManager.ApplyDailyTicks(character, gameDay)` from your day-advance handler to apply all daily job mechanics in one call.

---

## 12. Quests

Once accepted (`quest.Clone()` gives the character their own copy of the template), progress tracks itself with zero extra work from you: kill progress updates from the `MonsterKilled` event, item-collection progress from `ItemReceived`. When every objective is met, `Status` flips to `Completed` automatically and the quest can be turned in. Gating conditions (level, class, race, active job, a specific job-aspect level, party membership/size, prerequisite quests) are all optional and freely combinable — new gating combinations are pure data changes, never code changes. `QuestManager.GetAvailableForCharacter` and `GetAcceptableForNpc`/`GetReturnableForNpc` do the gating checks for you.

---

## 13. World — Rooms, NPCs, Maps

Rooms form a graph via their `Exits` dictionary (direction → room). Before moving a character, check `RoomService.CanEnterRoom(room, character)` — it enforces the room's `RequirementType` (`Level`, `Quest`, or `Party`; note the `Party` case currently checks *membership only*, not a minimum size — see [§21](#21-known-limitations)). Dungeon rooms spawn their monsters lazily via `room.SpawnDungeonMonsters()` on entry; random encounters are picked from `Room.EncounterableMonsters` (a weighted id→chance map) via `MonsterService.PickMonsterForFight`.

NPC interactions route through a single entry point, `NpcInteractionService.Execute(character, npc, serviceId, item, amount)`, dispatching on `serviceId` (`heal`, `buy_items`, `sell_items`, `upgrade`, `talk`; note `craft` is a stub that always fails — see [§15](#15-gathering--crafting) for the actual crafting path). This keeps your UI from needing a separate method per NPC service type.

For map rendering, `MapBuilder.BuildRoomMap()` lays out the known rooms around the current one as a 2D grid via breadth-first search — feed that straight into a UI grid.

---

## 14. Time & Ticks

Game time is a tick counter: a fixed number of ticks (`DayCycleManager.TicksPerSegment`, default 50) makes a segment (Morning → Noon → Evening → Night), four segments make a day. Actions cost varying ticks (`GameTick.RoomTraversal`, `.CombatVictory`, `.Gather`, `.Rest`, …) via `DayCycleManager.AddTicks(int)`. Subscribe to `SegmentChanged` and `DayAdvanced` to react to time passing — `DayAdvanced` in particular is where you should hook job decay, class inactivity penalties, and gather-limit resets. `DayCycleManager.StartInactivityTimer()` optionally advances time passively even without player action (useful for a desktop client; a server might drive this differently, or not at all).

---

## 15. Gathering & Crafting

`GatherService.Gather(character, room)` checks for the right tool, consumes one of the room's daily gather charges, grants job skill XP, and applies the job's skill multiplier to the yield. Each room rolls 1–5 base daily gather attempts; a higher job Knowledge level grants bonus charges (`JobManager.GetGatherKnowledgeBonus`). Failures return a specific `GatherResult` (`NoSpots`, `Depleted`, `NoTool`, `InventoryFull`) so your UI can react precisely instead of guessing.

Crafting is intentionally split: `CraftingService.GetRecipes(npcId)` / `GetRecipe(npcId, outputId)` give you the recipe (output item, ingredients, required job-knowledge level) and you check the character has the ingredients yourself — **actually consuming ingredients and creating the output item is left to your app**, since single-player (local) and multiplayer (server-authoritative) hosts need to execute that step differently. Do **not** route through `NpcInteractionService.Execute(character, npc, "craft", item)` / `Npc.CraftItem()` — that path is an unfinished stub that always returns failure (see [§21](#21-known-limitations)).

---

## 16. Rune Magic

Rune magic is a fourth skill system, separate from the three tiers in [§10](#10-skills--three-tiers), and it's fully functional at the library level (a specific consuming app might choose to gate it behind a particular class — that's an app-level UI decision, not a library limitation). A `CompositeRune` starts from a `BaseRuneData` template; the player adds any number of `RuneWord`s to it, and each addition triggers `RuneEvaluator.Evaluate()` to recompute its stats based on the relationship between the words already present:

| Relationship | Effect |
|---|---|
| Support | Increases the scaling factor |
| Contradiction | Also increases the scaling factor (different thematic flavor) |
| Neutral | Increases mana cost instead |
| Transform | Unlocks an entirely new rune |

The relationship between two words is resolved in this order: an explicit `WordPairRelation` entry first, then the word families' default relationship, then `Neutral` as the fallback. `RuneManager` also tracks a per-character translation system — until a word is officially learned, the player can assign it a personal guessed label (`SetCharacterLabel`) that displays in place of the raw runic script (`GetDisplayName`), supporting a "the language is being decoded gradually" narrative if you want one.

---

## 17. Accounts & Persistence

`LoginManager.Register(username, password)` / `.Login(username, password)` are the built-in file-based account system — passwords are hashed with PBKDF2-SHA512 (16-byte salt, 32-byte hash, 200,000 iterations), never stored in plaintext. Both methods are synchronous and **not** thread-safe against concurrent writes to the same account file, so if you're building a server, add your own locking or swap in a database-backed alternative.

`CharacterService.SaveCharacter(account, character)` / `LoadCharacter(name, account)` handle file-based character persistence under `Data/saves/{username}-{charactername}.json`, including restoring extended state (runes, fusion/combination skills, skill slots) on load via `SkillFusionSystem.ResolveCompositeSkills`.

This file-based persistence sits behind `ICharacterRepository` / `IUserRepository` interfaces, so you can swap in your own database-backed implementation without touching the rest of the library — this is exactly what MyriaServer (the reference project's ASP.NET Core backend) does with a SQL-backed repository.

---

## 18. Events Reference

MyriaLib has no UI, so it uses .NET events instead of polling to tell you when something worth reacting to happened.

**On `Character`:** `XpGained`, `LeveledUp`, `HealthChanged`, `ManaChanged`, `SkillLearned`

**On `Inventory`:** `ItemReceived`, `ItemRemoved`, `ItemSold`

**On `CombatEncounter` / `GroupCombatEncounter`:** `MonsterKilled`

**On `DayCycleManager`:** `SegmentChanged`, `DayAdvanced`

**`GameLog`** additionally keeps a general diagnostic event log (`EntryAdded`, `RecentEntries` for the last 20) independent of the above, useful for debugging/dev builds.

**`GameEvents` (static, `MyriaLib.Systems`)** is a second, mod-oriented event hub, distinct from the instance events above: `SessionStarted`, `LevelUp`, `ClassChanged`, `RoomEntered`, `DayAdvanced`, `MonsterKilled`, `ItemUsed`. It exists specifically so DLL mods have one static place to subscribe from inside `IModLoaderExtender.AfterModsLoaded` without needing a reference to a specific character instance — and without needing to unsubscribe, since mod assemblies are unloaded wholesale between sessions. Note `GameEvents.SessionStarted` and `GameService.SessionStarted` are two distinct events fired together by `GameService.StartSession` — the former for mod code, the latter for your application code.

---

## 19. Mod System

A mod is a folder under `Mods/` with a `mod.json` manifest (`id`, `name`, `version`, `enabled`, `loadOrder`) and a data tree mirroring `Data/`. `ModLoader.Load()` classifies every file a mod provides as visual or gameplay-relevant based on its path prefix (`Data/locales/`, `Assets/`, icon paths → visual; `Data/common/` → gameplay); if a mod contributes at least one gameplay file, the whole mod counts as gameplay-relevant. Overlapping files are resolved by `loadOrder` (higher wins) — full-file replacement only, no field-level merging. `ModLoader.ResolvePath(defaultPath)` is what every loader in [§4](#4-initialization--load-order) actually calls to get its real file path.

If you're building a networked game and want to guarantee clients can't gain an advantage from gameplay mods, call `ModLoader.ApplyMultiplayerMode(true)` before connecting — gameplay mods are skipped (visual-only mods stay active) so the client loads the same unmodified data as the server. `ModLoader.GetModInfo()` reports the client's active mods (including a SHA-256 fingerprint per gameplay mod) if your server wants to allowlist specific mods instead of blocking all of them.

For app-specific reactions to mod loading (e.g. swapping a WPF `ResourceDictionary` when a visual mod loads — something MyriaLib itself must never know about), implement `IModLoaderExtender` and register it with `ModLoader.RegisterExtender()`. If an extender throws during load, only that mod is unloaded — a single bad mod never blocks the rest of your game from starting.

---

## 20. Building a Multiplayer Server (`IGameDataSource`)

By default, every loader in `GameService.InitializeGame()` reads its JSON file straight off disk. If you're building a server that keeps its own authoritative copy of content — in a database, for example — implement `IGameDataSource` instead:

```csharp
public interface IGameDataSource
{
    List<RaceProfile> GetRaces();
    List<ClassProfile> GetClasses();
    List<MonsterLootTable> GetLootTables();
    List<GameItem> GetItems();
    List<Monster> GetMonsters();
    List<Npc> GetNpcs();
    List<Room> GetRooms();
    Dictionary<string, CraftingRecipe[]> GetRecipes();
    List<Job> GetJobs();
    List<Quest> GetQuests();
    List<SkillData> GetSkills();
    List<EffectDefinition> GetEffects();
    Dictionary<string, string[]> GetStartingItems();
    List<City> GetCities();
    List<Cave> GetCaves();
    List<Dungeon> GetDungeons();
    List<Forest> GetForests();
    (List<RuneWord> Words, List<WordFamily> Families, List<WordPairRelation> Pairs) GetRuneWords();
    List<BaseRuneData> GetBaseRunes();
    List<BaseSkillData> GetBaseSkills();
    List<FusionRecipe> GetFusionRecipes();
    List<SkillCombinationRecipe> GetSkillCombinations();
}
```

Pass an instance to `GameService.InitializeGame(progress: null, source: myDataSource)` and every loader calls the matching data-based overload instead of touching JSON at all — mod path resolution is skipped entirely in that case. The load order (documented in [§4](#4-initialization--load-order)) is identical either way, since both branches go through the same method — you only ever need to get the *data* right, not re-derive the *order*.

This is exactly the pattern the reference Myria project uses: MyriaLib has zero database-specific code; the server project owns a `SqlGameDataSource` that reads from SQL and hands MyriaLib plain in-memory lists, keeping persistence technology entirely outside the library.

For character save data specifically (rather than static world content), implement `ICharacterRepository`/`IUserRepository` instead — see [§17](#17-accounts--persistence).

---

## 21. Known Limitations

Being upfront about the rough edges so you don't lose time rediscovering them:

- **Fusion skills have no creation entry point.** `SkillFusionSystem.TryCreateForCharacter` exists and works correctly, but nothing in the library calls it — you must build your own UI/API for it, exactly as you would for skill combination (`SkillCombinationService.TryCreateForCharacter`). The display and combat-slotting code for fusion skills is complete; only "let the player actually make one" is missing.
- **`base_skills.json` and `fusion_recipes.json` aren't included** in this repo's sample `Data/` folder, even though `InitializeGame()` loads them unconditionally. The loaders fail soft (log + empty collection) rather than crash, so you won't notice unless you try to use fusion skills — at which point there will simply be no components or recipe overrides available. If you want this feature, you'll need to author these files yourself.
- **`equipment_slots.json`, `item_rarities.json`, `time_segments.json`, `gathering_types.json`** (the enum display-name registries) are likewise not included and not auto-loaded — only relevant if you call the corresponding `GameConfig.Load...()` methods.
- **NPC crafting via `NpcInteractionService.Execute(..., "craft", ...)` / `Npc.CraftItem()` is an unimplemented stub** that always returns failure. Use `CraftingService` directly instead (see [§15](#15-gathering--crafting)).
- **Room party-size gating is incomplete.** `Quest` has a `RequiredPartySize` field, but `Room`'s `RequirementType.Party` only checks party *membership*, not a minimum size — there's no equivalent field on `Room` yet.
- **Everything is a static, process-wide singleton.** There's no way to run two independent "game worlds" in the same process (e.g. two isolated test instances) — every loaded room, item, quest, etc. lives in static state shared across your whole process. Design your hosting accordingly (typically: one process per server instance).
- **No async I/O anywhere.** Loading and persistence are synchronous. Fine for a desktop client; if you're building a high-throughput server, benchmark before assuming this is a non-issue.
- **`LoginManager`'s file-based accounts aren't safe for concurrent writes** to the same account file — treat it as a single-player/prototype convenience, not production account storage, unless you add your own locking or replace it with a database via `IUserRepository`.
- **Test coverage is a foundation, not exhaustive.** `MyriaLib.Tests` (xUnit) covers combat formulas, money/inventory, skill fusion, rune evaluation, class progression, and config forwarding — but quests, jobs, gathering/crafting, world navigation, and full combat-encounter flow have no tests yet. Because MyriaLib leans on static, process-wide state, test parallelization is disabled assembly-wide and any test that touches shared static services (`ClassProfile`, `GameConfig`-backed values, etc.) must snapshot and restore it — see the existing tests for the pattern before adding more.

---

## License

MIT — see [LICENSE](LICENSE). Use it in your own games, commercial or not, modify it freely; just keep the copyright notice.
