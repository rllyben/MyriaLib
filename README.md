# MyriaLib - Game Library

A C# game-logic library for text-based or hybrid RPG games. Handles all back-end systems — combat, progression, quests, jobs, inventory, world navigation — so the consuming application only needs to build the UI layer on top.

---

## Table of Contents

1. [Overview](#1-overview)
2. [Quick Start](#2-quick-start)
3. [Initialization & Load Order](#3-initialization--load-order)
4. [Configuration (GameConfig)](#4-configuration-gameconfig)
5. [Data Files — JSON Reference](#5-data-files--json-reference)
6. [Player & Character](#6-player--character)
7. [Inventory & Economy](#7-inventory--economy)
8. [Combat](#8-combat)
9. [Skills](#9-skills)
10. [Jobs](#10-jobs)
11. [Quests](#11-quests)
12. [World — Rooms, NPCs, Maps](#12-world--rooms-npcs-maps)
13. [Time Cycle & Ticks](#13-time-cycle--ticks)
14. [Gathering & Crafting](#14-gathering--crafting)
15. [Rune Magic](#15-rune-magic)
16. [Progression — XP & Levels](#16-progression--xp--levels)
17. [Authentication & Persistence](#17-authentication--persistence)
18. [Events Reference](#18-events-reference)
19. [Enum Registries](#19-enum-registries)
20. [Localization](#20-localization)
21. [Mod System](#21-mod-system)

---

## 1. Overview

MyriaLib is a self-contained game engine library. It defines all entity types (Player, Monster, Item, Room, NPC, Quest, Skill, Rune…), the services that operate on them, and the static managers that coordinate system-level rules (XP curves, daily decay, combat formulas, etc.).

**What the library handles:**
- Character creation, progression, class and job switching
- Inventory management, stacking, selling, equipment
- Turn-based single and group combat
- Three-tier skill system: regular, combined, and fusion skills
- Rune magic with a word-pair relationship engine
- Quest system with full gating (level, class, race, job aspects, party, prerequisites)
- Job system with three independent aspects (Skill, Knowledge, Fame) and daily mechanics
- World graph (rooms, exits, NPCs, gathering spots, dungeon spawning)
- In-game time (tick-driven day/night cycle)
- JSON-file persistence for users, characters, and game state
- Localization (English, German)

**What the library does NOT handle:**
- UI rendering of any kind
- Networking / multiplayer transport
- Audio
- Asset loading beyond JSON data files

---

## 2. Quick Start

### Prerequisites
- .NET 8 or later
- A `Data/` directory tree with the JSON content files (see §5)

### Minimal setup

```csharp
using MyriaLib.Systems;
using MyriaLib.Services.Manager;

// 0. Load mods (optional — silently skips if Mods/ does not exist)
ModLoader.Load("Mods");

// 1. Tune any values you want to change before loading data
GameConfig.SetCurrencyRatios(1000, 1000, 100, 100);   // defaults — optional
GameConfig.SetClassProgression(maxLevel: 50, xpCostBase: 5_000);

// 2. Initialize the game world — every data file is automatically resolved
//    through ModLoader.ResolvePath(), so mod overrides are applied transparently.
GameService.InitializeGame();

// 4. Log a user in
var result = await LoginManager.Login("username", "password");
if (result.Success)
{
    var player = await CharacterService.LoadCharacter("HeroName", result.Account);
    DayCycleManager.StartInactivityTimer(); // optional passive time
}
```

---

## 3. Initialization & Load Order

`GameService.InitializeGame()` calls all sub-loaders in the correct order. You do not need to call individual loaders manually for runtime data. Every path is resolved through `ModLoader.ResolvePath()` before loading, so any active mod overrides are applied transparently.

**Call `ModLoader.Load()` before `InitializeGame()`** so the path resolver is populated in time.

The load sequence inside `InitializeGame()` is:

1. `RaceProfile.Load()` — race stat profiles
2. `ClassProfile.Load()` — class stat profiles
3. `LootGenerator.Load()` — type-based loot tables
4. `ItemFactory.LoadItems()`
5. `MonsterService.LoadMonsters()`
6. `NpcService.LoadNpcs()`
7. `RoomService.LoadRooms()` — also links exits, wires monsters and NPCs to rooms
8. `DayCycleManager.Initialize()` — rolls daily gather limits
9. `CraftingService.LoadRecipes()`
10. `JobManager.LoadJobs()`
11. `QuestManager.LoadQuests()`
12. `SkillFactory.LoadSkills()`
13. Map registries (Cave, City, Dungeon, Forest)
14. `RuneWordService.Load()` — rune words, families, pairs
15. `BaseRuneService.Load()`
16. `BaseSkillLoader.Load()`
17. `FusionRecipeService.Load()`
18. `SkillCombinationService.Load()`

**Enum registries** (optional display-name overlays) are not loaded by `InitializeGame()` — call these manually if your UI uses them:
- `GameConfig.LoadEquipmentSlots(path)`, `LoadItemRarities(path)`, `LoadTimeSegments(path)`, `LoadGatheringTypes(path)`

**Multiplayer mode**: before entering a server session, set `ModLoader.MultiplayerMode = true` and re-call `InitializeGame()`. Gameplay mod overrides will be bypassed automatically; visual mods remain active. Reset to `false` and re-call on disconnect to restore mods.

---

## 4. Configuration (GameConfig)

`GameConfig` (in `MyriaLib.Systems`) is the single entry point for all tunable values. Call these methods once at application startup before `InitializeGame()`.

### Currency ratios

```csharp
GameConfig.SetCurrencyRatios(
    bronzePerSilver:    1_000,   // default
    silverPerGold:      1_000,
    goldPerPlatinum:    100,
    platinumPerCrystal: 100
);
```

Modifies `Money.BRONZE_PER_SILVER` etc. All derived ratios (`BRONZE_PER_GOLD`, `BRONZE_PER_PLATINUM`, `BRONZE_PER_CRYSTAL`) recompute automatically.

### Inventory page size

```csharp
GameConfig.SetInventoryPageSize(49); // default — 7×7 grid
```

### Class progression

```csharp
GameConfig.SetClassProgression(
    maxLevel:    50,     // default
    xpCostBase:  5_000   // XP to advance from level N = N × xpCostBase
);
```

### Job progression

```csharp
GameConfig.SetJobProgression(
    maxLevel:      100,   // default
    xpCostBase:    1_000,
    maxFameBonus:  1.5,   // fame multiplier: ×1.0 → ×(1.0 + maxFameBonus) = ×2.5
    maxSkillBonus: 2.0    // skill multiplier: ×1.0 → ×3.0
);
```

### Gather bonus thresholds

Knowledge-level thresholds that grant extra daily gather charges. Sorted ascending.

```csharp
GameConfig.SetGatherBonusThresholds(new[] {
    (Level: 10,  Bonus: 1),
    (Level: 30,  Bonus: 2),
    (Level: 60,  Bonus: 3),
    (Level: 100, Bonus: 4),
});
```

### Equipment upgrade gates

Knowledge-level gates controlling max equipment upgrade level.

```csharp
GameConfig.SetUpgradeGates(
    defaultMax: 2,
    gates: new[] {
        (Level: 10,  MaxUpgrade: 4),
        (Level: 30,  MaxUpgrade: 6),
        (Level: 60,  MaxUpgrade: 8),
        (Level: 100, MaxUpgrade: 10),
    }
);
```

### Cooldowns

```csharp
GameConfig.SetClassCooldown(TimeSpan.FromDays(7));   // default
GameConfig.SetJobCooldown(TimeSpan.FromDays(7));
```

### Class penalty

```csharp
GameConfig.SetClassPenaltyPerDay(500L); // XP lost per day from inactive classes
```

### Job daily mechanics

```csharp
GameConfig.SetJobDailyMechanics(
    fameTickPerDay:         5,    // passive Fame XP/day for active job
    skillDecayCap:          50,   // max Skill XP lost/day when unused
    fameDecayDivisor:       200,  // Fame decay = FameXp / this (~0.5%/day)
    activeJobBonusFraction: 0.5   // Skill XP bonus for active job (0.5 = +50%)
);
```

### Action tick costs

```csharp
GameTick.RoomTraversal = 2;   // ticks spent moving between rooms
GameTick.NpcInteraction = 5;
GameTick.CombatVictory  = 8;
GameTick.Gather         = 6;
GameTick.Rest           = 50;
```

### Loading enum profiles

```csharp
GameConfig.LoadRaces("Data/common/races.json");
GameConfig.LoadClasses("Data/common/classes.json");
GameConfig.LoadEquipmentSlots("Data/common/equipment_slots.json");
GameConfig.LoadItemRarities("Data/common/item_rarities.json");
GameConfig.LoadTimeSegments("Data/common/time_segments.json");
GameConfig.LoadGatheringTypes("Data/common/gathering_types.json");
LootGenerator.Load("Data/common/loot_tables.json");
```

---

## 5. Data Files — JSON Reference

All data files use camelCase keys. Deserialization is case-insensitive. Default base paths are `Data/common/` and `Data/users/`, `Data/saves/`. Override any path via the corresponding `Load(path)` method.

---

### 5.1 `items.json`

Array of item definitions.

```json
[
  {
    "id":          "iron_sword",
    "name":        "Iron Sword",
    "description": "A sturdy iron blade.",
    "type":        "equipment",
    "rarity":      "Common",
    "buyPrice":    500,
    "stackSize":   1,
    "maxStackSize": 1,
    "slotType":    "Weapon",
    "upgradeCategory": "blacksmith",
    "allowedClasses": ["Fighter", "Knight", "Barbarian"],
    "baseBonusATK": 12,
    "baseBonusDEF": 0,
    "baseBonusSTR": 2
  },
  {
    "id":          "health_potion",
    "type":        "consumable",
    "rarity":      "Common",
    "buyPrice":    100,
    "maxStackSize": 20,
    "healAmount":  50,
    "manaRestore": 0
  },
  {
    "id":          "iron_ore",
    "type":        "material",
    "rarity":      "Common",
    "buyPrice":    20,
    "maxStackSize": 99,
    "toolType":    "Ore"
  },
  {
    "id":          "page_expansion",
    "type":        "inventory_expansion",
    "buyPrice":    2000
  }
]
```

**Item types:** `equipment`, `consumable`, `material`, `inventory_expansion`

**Equipment-specific fields:** `slotType` (Weapon/Armor/Accessory), `upgradeCategory` (blacksmith/tailor/artificer), `baseBonusHP`, `baseBonusMP`, `baseBonusSTR`, `baseBonusDEX`, `baseBonusEND`, `baseBonusINT`, `baseBonusSPR`, `baseBonusATK`, `baseBonusDEF`, `baseBonusMATK`, `baseBonusMDEF`, `baseBonusAim`, `baseBonusEvasion`, `baseBonusCrit`, `baseBonusBlock`

**Tool items:** Set `toolType` to `Ore`, `Tree`, or `Herb` to mark a gathering tool.

**Optional:** `jobId` — links an item to a job (applies fame bonus on sell).

---

### 5.2 `monsters.json`

Array of monster templates.

```json
[
  {
    "id":          "goblin",
    "name":        "Goblin",
    "description": "A small green creature.",
    "type":        "Humanoid",
    "level":       5,
    "exp":         80,
    "minLoot":     10,
    "maxLoot":     50,
    "dropsCorpse": true,
    "stats": {
      "strength":    8,
      "dexterity":   10,
      "endurance":   6,
      "intelligence":3,
      "spirit":      2,
      "baseHealth":  120,
      "baseMana":    20
    },
    "uniqueLootTable": [
      { "itemId": "goblin_ear", "dropChance": 0.4 }
    ]
  }
]
```

**Monster types:** `Beast`, `Spirit`, `Elemental`, `Shadow`, `Undead`, `Humanoid`

`dropsCorpse`: set `false` for incorporeal types (Spirit, Shadow) — no looting UI is shown.

`uniqueLootTable`: per-monster individual drops, rolled independently from the type-based table.

---

### 5.3 `rooms.json`

Array of room definitions.

```json
[
  {
    "id":          "town_square",
    "name":        "Town Square",
    "description": "The heart of the town.",
    "isCity":      true,
    "exits": {
      "north": "north_gate",
      "east":  "market_street"
    },
    "npcs":     ["merchant_jan", "healer_mira"],
    "monsters": [],
    "gatheringSpots": [],
    "requirementType": "None"
  },
  {
    "id":            "iron_mine_entrance",
    "name":          "Iron Mine Entrance",
    "isCaveRoom":    true,
    "requirementType": "Level",
    "accessLevel":   10,
    "exits": { "south": "town_square" },
    "gatheringSpots": [
      {
        "id":             "vein_1",
        "name":           "Iron Vein",
        "type":           "Ore",
        "gatheredItemId": "iron_ore",
        "requiredToolId": "iron_pickaxe"
      }
    ],
    "encounterable_monsters": {
      "mine_bat": 3,
      "rock_crab": 1
    }
  }
]
```

**Room flags:** `isCity`, `isCaveRoom`, `isDungeonRoom`, `isBossRoom`

**Access gating:**
- `requirementType`: `None`, `Level`, `Quest`, `Party`
- `accessLevel`: minimum player level (for Level type)
- `requiredQuestId`: quest that must be completed (for Quest type)

**`encounterable_monsters`:** dict of monster ID → spawn weight. Used for random encounter selection.

---

### 5.4 `npcs.json`

```json
[
  {
    "id":          "healer_mira",
    "nameKey":     "npc.healer_mira.name",
    "descriptionKey": "npc.healer_mira.desc",
    "type":        "Healer",
    "services":    ["heal", "buy_items"],
    "itemNames":   ["health_potion", "mana_potion"]
  },
  {
    "id":          "master_smith",
    "type":        "Smith",
    "services":    ["upgrade", "craft"],
    "upgradeCategory": "blacksmith",
    "masterJobId": "blacksmith"
  }
]
```

**NPC services:** `heal`, `buy_items`, `sell_items`, `upgrade`, `craft`, `talk`

`masterJobId`: If set, completing quests from this NPC grants Knowledge XP for that job.

---

### 5.5 `jobs.json`

```json
[
  {
    "id":          "miner",
    "name":        "Miner",
    "description": "Masters of ore extraction.",
    "type":        "Gathering"
  },
  {
    "id":          "blacksmith",
    "name":        "Blacksmith",
    "type":        "Crafting"
  }
]
```

Job types are informational — they control which NPC master grants Knowledge XP, and the `type` field is available for UI filtering.

---

### 5.6 `quests.json`

```json
[
  {
    "id":          "q_first_ore",
    "name":        "First Steps",
    "description": "Collect some ore for the smith.",
    "giverNpcId":  "master_smith",
    "returnNpcId": "master_smith",
    "requiredLevel": 5,
    "isTalkOnly":  false,
    "requiredItems":  { "iron_ore": 5 },
    "requiredKills":  {},
    "rewardXp":    200,
    "rewardGold":  150,
    "rewardItems": ["iron_ingot"],
    "jobKnowledgeRewardJobId":    "miner",
    "jobKnowledgeRewardAmount":   500,
    "acceptDialog": [
      { "speaker": "npc",    "text": "quest.first_ore.accept_1" },
      { "speaker": "player", "text": "quest.first_ore.accept_2" }
    ],
    "returnDialog": [
      { "speaker": "npc", "text": "quest.first_ore.return_1" }
    ],
    "prerequisiteQuestIds": [],
    "isRepeatable":  false
  }
]
```

**Gating fields** (all optional):
- `requiredLevel`, `requiredClass`, `requiredRace`
- `requiredAspectJobId` + `requiredSkillLevel` / `requiredKnowledgeLevel` / `requiredFameLevel`
- `requiredActiveJobId` — player must have this job active
- `requiresParty: true` + `requiredPartySize: 3`
- `prerequisiteQuestIds: ["q_intro"]`

**Repeatable quests:**
```json
{
  "isRepeatable":      true,
  "repeatMaxLevel":    50,
  "repeatDailyLimit":  3,
  "repeatTotalLimit":  0
}
```

`repeatTotalLimit: 0` = unlimited.

---

### 5.7 `skills.json`

```json
[
  {
    "id":            "power_strike",
    "name":          "Power Strike",
    "description":   "A heavy physical blow.",
    "class":         "Fighter",
    "manaCost":      15,
    "type":          "Physical",
    "target":        "SingleEnemy",
    "scalingFactor": 1.8,
    "statToScaleFrom": "ATK",
    "minLevel":      1,
    "isHealing":     false,
    "castTime":      0,
    "recoveryTime":  1
  }
]
```

**Types:** `Physical`, `Magical`

**Targets:** `SingleEnemy`, `AllEnemies`, `Self`, `SingleAlly`

---

### 5.8 `base_skills.json`

Fusible skill components for the fusion system.

```json
[
  {
    "id":            "fire_core",
    "name":          "Fire Core",
    "description":   "A base fire skill component.",
    "class":         "ElementalMage",
    "componentType": ["Fire", "Magic"],
    "manaCost":      10,
    "scalingFactor": 1.2,
    "statToScaleFrom": "MATK",
    "requiredLevel": 1
  }
]
```

**Component types:** `Fire`, `Water`, `Earth`, `Wind`, `AOE`, `Heal`, `Magic`, `Physical`, `Self`, `Area`, `MultiHit`, etc.

---

### 5.9 `base_runes.json`

```json
[
  {
    "id":              "fire_rune",
    "name":            "Ignis",
    "description":     "A rune of fire.",
    "coreWordId":      "word_ignis",
    "class":           "RunicMage",
    "baseManaCost":    20,
    "baseScalingFactor": 1.0,
    "statToScaleFrom": "MATK",
    "target":          "SingleEnemy",
    "isHealing":       false
  }
]
```

---

### 5.10 Rune Word Files

#### `rune_words.json`
```json
[
  {
    "id":          "word_ignis",
    "englishName": "Ignis",
    "runicScript": "ᛁᚷᚾᛁᛋ",
    "familyId":    "family_fire"
  }
]
```

#### `rune_families.json`
```json
[
  {
    "id":   "family_fire",
    "name": "Fire",
    "familyRelations": {
      "family_water": "Contradiction",
      "family_amplify": "Support"
    }
  }
]
```

#### `rune_word_pairs.json`
Explicit word-pair overrides (take precedence over family defaults).
```json
[
  {
    "wordIdA":     "word_ignis",
    "wordIdB":     "word_aqua",
    "relationship": "Transform",
    "transformResultRuneId": "steam_rune"
  }
]
```

**Relationships:** `Support` (boosts scaling), `Contradiction` (also boosts scaling, different flavor), `Neutral` (adds mana cost), `Transform` (unlocks a new rune when both words are present).

---

### 5.11 `skill_combinations.json`

Named overrides for combined skills. Without an entry, the algorithm generates a fallback.

```json
[
  {
    "inputSkillIds":   ["power_strike", "battle_cry"],
    "resultId":        "war_shout",
    "resultName":      "War Shout",
    "resultDescription": "A battle cry that empowers your strike.",
    "scalingFactorOverride": 2.1,
    "manaCostOverride": 25
  }
]
```

---

### 5.12 `fusion_recipes.json`

Named overrides for fusion skills.

```json
[
  {
    "resultId":          "inferno",
    "resultName":        "Inferno",
    "resultDescription": "A devastating fusion of fire components.",
    "componentIds":      ["fire_core", "heat_amplifier"],
    "scalingFactorOverride": 2.5,
    "targetOverride":    "AllEnemies"
  }
]
```

---

### 5.13 `recipes.json` (Crafting)

```json
[
  {
    "npcId":       "master_smith",
    "outputItemId":"iron_sword",
    "ingredients": [
      { "itemId": "iron_ingot", "amount": 3 }
    ]
  }
]
```

---

### 5.14 `races.json`

Loaded via `GameConfig.LoadRaces()`. Defines all playable races.

```json
[
  {
    "race":         "Myralu",
    "baseStatBonus": { "STR": 3, "DEX": 1, "END": 7, "INT": 3, "SPR": 3 },
    "baseHpBonus":  30,
    "baseManaBonus": 20,
    "statGrowth":   { "STR": 1, "DEX": 1, "END": 2, "INT": 2, "SPR": 2 },
    "hpPerLevel":   6,
    "manaPerLevel": 5,
    "forbiddenClasses": ["RunicMage"]
  }
]
```

`forbiddenClasses` lists `PlayerClass` enum names that this race cannot select.

---

### 5.15 `classes.json`

Loaded via `GameConfig.LoadClasses()`. Defines all playable classes.

```json
[
  {
    "class":       "Fighter",
    "statGrowth":  { "STR": 4, "DEX": 2, "END": 2, "INT": 1, "SPR": 2 },
    "hpPerLevel":  8,
    "manaPerLevel": 5
  }
]
```

---

### 5.16 `loot_tables.json`

Loaded via `LootGenerator.Load()`. Defines type-based drop tables for monster types.

```json
[
  {
    "monsterType": "Beast",
    "drops": [
      { "itemId": "beast_flesh",   "dropChance": 0.6 },
      { "itemId": "feral_leather", "dropChance": 0.4 },
      { "itemId": "beast_fang",    "dropChance": 0.2 }
    ]
  },
  {
    "monsterType": "Spirit",
    "drops": [
      { "itemId": "spirit_dust", "dropChance": 0.7 }
    ]
  }
]
```

`dropChance` is 0.0–1.0, rolled independently for each entry.

---

### 5.17 Enum Definition Files

Used to supply display names and ordering for enum values. All share the same format:

```json
[
  { "id": "Weapon",    "displayName": "Weapon Slot", "order": 0 },
  { "id": "Armor",     "displayName": "Armor Slot",  "order": 1 },
  { "id": "Accessory", "displayName": "Accessory",   "order": 2 }
]
```

| File | Default path | Registry class |
|------|-------------|----------------|
| Equipment slots | `Data/common/equipment_slots.json` | `EquipmentTypeRegistry` |
| Item rarities | `Data/common/item_rarities.json` | `ItemRarityRegistry` |
| Time segments | `Data/common/time_segments.json` | `TimeSegmentRegistry` |
| Gathering types | `Data/common/gathering_types.json` | `GatheringTypeRegistry` |

The `id` field must match the C# enum name exactly.

---

## 6. Player & Character

### Player entity (`MyriaLib.Entities.Players.Player`)

`Player` inherits from `CombatEntity` and aggregates all character state.

**Core state:**
```
player.Name        // character name
player.Race        // PlayerRace enum
player.Class       // PlayerClass enum
player.Level       // current character level
player.Experience  // total accumulated XP
player.Stats       // Stats object (STR, DEX, END, INT, SPR + bonuses)
player.CurrentHealth / CurrentMana
player.MaxHealth   / MaxMana
```

**Equipment:**
```
player.WeaponSlot     // EquipmentItem? (null if empty)
player.ArmorSlot
player.AccessorySlot
```

**Combat totals** (read-only computed properties):
```
player.TotalPhysicalAttack   // base ATK + gear + stat scaling
player.TotalMagicAttack
player.TotalPhysicalDefense
player.TotalMagicDefense
player.TotalAim / TotalEvasion
player.CritChance / BlockChance
```

**Skills:**
```
player.Skills            // List<Skill>  — learned base skills
player.CombinedSkills    // List<CombinedSkill>
player.CompositeSkills   // List<CompositeSkill>
player.KnownRunes        // List<CompositeRune>
player.SkillSlots        // List<SkillSlot>  — combat bar
```

**Jobs:**
```
player.Jobs         // List<PlayerJob>  — one entry per job the player has touched
player.ActiveJobId  // string?
```

**Quests:**
```
player.ActiveQuests     // List<Quest>
player.CompletedQuests  // List<Quest>
```

**Navigation:**
```
player.CurrentRoom    // Room
player.CurrentRoomId  // int
```

### Key player methods

```csharp
player.GainXp(long amount);               // may trigger level-up
player.Heal(int amount);                  // capped at MaxHealth
player.ApplyDamage(int amount);           // fires HealthChanged event
player.SpendMana(int amount);             // fires ManaChanged event
player.RestoreMana(int amount);
player.LearnSkill(Skill skill);           // no-op if already known
player.HasToolFor(GatheringType type);    // checks inventory + weapon slot
```

### Creating a new character

```csharp
var player = new Player
{
    Name       = "Hero",
    Race       = PlayerRace.Myralu,
    Class      = PlayerClass.Fighter,
    Level      = 1,
    Experience = 0,
};

// Apply race base stats
// (done internally by Player.LevelUp() on first level or by your creation flow)

SkillFactory.UpdateSkills(player);         // grants class skills at level 1
BaseRuneService.GrantBaseRunes(player);    // grants runic class starting runes

await characterRepository.SaveAsync(username, player);
```

### Loading an existing character

```csharp
var player = await CharacterService.LoadCharacter("HeroName", userAccount);
// CharacterService.ResolveAdvancedSystems() is called automatically,
// restoring rune, fusion, combination, and slot data.
```

---

## 7. Inventory & Economy

### Inventory

```csharp
var inv = player.Inventory;

// Add an item
bool added = inv.AddItem(item, player);

// Remove an item
bool removed = inv.RemoveItem(item);

// Use a consumable by name
bool used = inv.UseItem("health potion", player);

// Equip/unequip
bool equipped = inv.SwapEquipment("iron sword", player);

// Sell (charges money to NPC, applies fame bonus)
bool sold = inv.SellItem("iron ore", quantity: 5, ref player);

// Page access (for 7×7 UI grids)
Item?[] page0 = inv.GetPage(0);   // 49 slots, nulls for empty
int usedPages  = inv.UsedPages;
int capacity   = inv.Capacity;     // Pages × PageSize
```

**Events:**
```csharp
inv.ItemReceived += (_, e) => Console.WriteLine($"Got {e.Item.Name} ×{e.StackSize}");
inv.ItemRemoved  += (_, e) => { };
inv.ItemSold     += (_, e) => { };
```

### Money

`Money` is an immutable struct stored in base Bronze units.

```csharp
// Construct
var m = new Money(5_000);                            // 5,000 Bronze
var m = Money.FromComponents(crystals:1, platinum:0, gold:0, silver:0, bronze:0);

// Display
m.ToString("S");  // "5 S" (short — skips zero groups)
m.ToString("L");  // "0 Coin Crystals 0 Platinum 0 Gold 5 Silver 0 Bronze"
m.ToString("C");  // "5 Silver" (compact — top two non-zero)
m.ToString("B");  // "5,000 Bronze"

// Parse
Money parsed = MoneyFormatter.Parse("2 G 500 S");

// Arithmetic
Money total = money1 + money2;
bool affordable = money1 >= money2;
```

### MoneyBag (player wallet)

```csharp
var wallet = player.Money;

bool ok     = wallet.CanAfford(bronzeAmount);
bool added  = wallet.TryAdd(bronzeAmount);
bool spent  = wallet.TrySpend(bronzeAmount);
long balance = wallet.Balance.BronzeTotal;
```

---

## 8. Combat

### Single combat (`CombatEncounter`)

```csharp
// Start encounter with a monster in the current room
var encounter = new CombatEncounter(player, monster);

// Player turn — one of:
encounter.PlayerAttack();                    // basic attack
encounter.PlayerBeginCast(skill);            // start casting a skill
encounter.PlayerUseItem("health_potion");    // use a consumable

// If casting, advance each game tick until cast is complete
encounter.Tick();                            // decrements cast/recovery counters

// Check state
encounter.Phase     // CombatPhase: PlayerTurn | Casting | Recovery | Finished
encounter.Log       // List<CombatLogEntry>  — localization keys + args
encounter.InventoryFull  // true if loot couldn't fit

// On victory, loot is already in player.Inventory
// Quest kill progress is updated automatically via MonsterKilled event
```

**Events:**
```csharp
encounter.MonsterKilled += (_, e) => Console.WriteLine($"{e.MonsterId} defeated");
```

### Group combat (`GroupCombatEncounter`)

Follows the same API but accepts `List<Player>` and `List<Monster>`.

```csharp
var encounter = new GroupCombatEncounter(players, monsters);
string currentPlayer = encounter.CurrentTurnPlayerName;
// actions are attributed to the current player
```

### Combat formulas (read-only)

`CombatSystem` exposes the raw calculation methods for display/preview:

```csharp
bool hits  = CombatSystem.TryHit(attacker, defender);
int damage = CombatSystem.CalculateDamage(attacker, defender);
```

---

## 9. Skills

### Three skill tiers

| Tier | Class | How created |
|------|-------|-------------|
| Regular | `Skill` | Learned automatically by `SkillFactory.UpdateSkills()` on level-up or class change |
| Combined | `CombinedSkill` | Player combines 2–5 regular skills |
| Fusion | `CompositeSkill` | Player fuses base skill components |

Rune magic is a separate fourth system (§15).

### Regular skills

```csharp
// Update player's known skills (call on level-up or class change)
SkillFactory.UpdateSkills(player);

// List skills available at current class/level
var available = SkillFactory.GetSkillsFor(player);
```

### Combined skills

```csharp
// Combine two or more skills
var result = SkillCombinationService.TryCreateForPlayer(
    player,
    skillIds: new[] { "power_strike", "battle_cry" }
);
// result contains the new CombinedSkill, or null if already exists / invalid
```

Rules: 2–5 skills, all from the same class, unique combination. AoE combinations take a –10% scaling penalty; single-target get +10%.

### Fusion skills (composite)

```csharp
// Fuse base skill components
bool created = SkillFusionSystem.TryCreateForPlayer(
    player,
    componentIds: new[] { "fire_core", "heat_amplifier" }
);
```

### Skill bar (SkillSlots)

The combat skill bar holds slotted skills across all tiers.

```csharp
// Slot a regular skill
SkillSlotService.TryAddSlot(player, SlottedSkillSource.Regular, "power_strike");

// Slot a combined skill
SkillSlotService.TryAddSlot(player, SlottedSkillSource.Combined, combinedSkill.Id);

// Remove
SkillSlotService.RemoveSlot(player, SlottedSkillSource.Regular, "power_strike");

// Reorder (swap positions)
SkillSlotService.ReorderSlots(player, fromIndex: 0, toIndex: 2);

// Get all combat skills with resolved Skill objects
var slots = SkillSlotService.GetCombatSkills(player);
```

`player.SkillSlotCount` is the maximum number of slots (grows with level). `player.FusionSlotCount` caps fusion skill slots separately.

---

## 10. Jobs

### Overview

Each player can have multiple jobs. Only one is "active" at a time. Each job tracks three independent aspects:

| Aspect | Gained by | Lost by |
|--------|-----------|---------|
| **Skill XP** | Gathering / crafting (+50% when active) | Daily decay if unused (up to 50 XP/day) |
| **Knowledge XP** | Job master quests | Resets to level floor daily (levels preserved) |
| **Fame XP** | Active-job activities + 5 XP/day passive | ~0.5%/day decay when not active |

### Job API

```csharp
// Switch active job (7-day cooldown enforced)
bool ok = JobManager.SetActiveJob(player, "miner");
bool ok = JobManager.SetActiveJob(player, null);  // clear job (always free)

// Grant XP (handles active-job bonus automatically)
JobManager.GrantSkillXp(player, "miner", amount: 200);
JobManager.GrantKnowledgeXp(player, "miner", amount: 500);
JobManager.GrantFameXp(player, "miner", amount: 100);  // only if active

// Derived benefits
double sellMult = JobXpService.GetFameMultiplierFromXp(entry.FameXp);  // e.g. ×1.7
double gatherMult = JobXpService.GetSkillMultiplierFromXp(entry.SkillXp);
int    extraGathers = JobManager.GetGatherKnowledgeBonus(player);
int    maxUpgrade   = JobXpService.GetMaxUpgradeLevel(knowledgeLevel);

// Daily tick (call from DayCycleManager.DayAdvanced event)
JobManager.ApplyDailyTicks(player, gameDay);

// Level info
int level = JobXpService.GetLevel(entry.SkillXp);
string progress = JobXpService.FormatProgress(entry.SkillXp); // "1,200 / 5,000 XP"
```

### Cooldown checks

```csharp
bool canSwitch = JobManager.CanChangeJob(player);
TimeSpan remaining = JobManager.GetCooldownRemaining(player);
```

---

## 11. Quests

### Quest lifecycle

1. `QuestManager.GetAcceptableForNpc(player, npcId, partySize)` — quests available at this NPC
2. Player accepts → `quest.Clone()` is added to `player.ActiveQuests`; `quest.GrantAcceptItems(player)` fires
3. Quest tracks progress automatically:
   - Kill progress: updated by `CombatEncounter.MonsterKilled` event (wired internally)
   - Item progress: updated by `Inventory.AddItem` (wired internally)
4. Quest auto-completes when all objectives are met (status → `QuestStatus.Completed`)
5. `QuestManager.GetReturnableForNpc(player, npcId)` — quests ready to turn in
6. Player turns in → `quest.GrantRewards(player)` fires

### Quest API

```csharp
// Get available quests at an NPC
var available = QuestManager.GetAvailableForPlayer(player, partySize: 1);
var npcQuests  = QuestManager.GetAcceptableForNpc(player, "master_smith", partySize: 1);

// Accept
var questCopy = questTemplate.Clone();
questCopy.GrantAcceptItems(player);
player.ActiveQuests.Add(questCopy);

// Turn in
questCopy.GrantRewards(player);
player.ActiveQuests.Remove(questCopy);
player.CompletedQuests.Add(questCopy);
```

### Quest gating summary

All gating fields are optional. Omit or set to 0/null to skip that gate.

| Field | Effect |
|-------|--------|
| `requiredLevel` | Minimum player level |
| `requiredClass` | Must be this class |
| `requiredRace` | Must be this race |
| `requiredActiveJobId` | Must have this job active |
| `requiredAspectJobId` + `requiredSkillLevel` / `requiredKnowledgeLevel` / `requiredFameLevel` | Job aspect gate |
| `requiresParty` + `requiredPartySize` | Party size gate |
| `prerequisiteQuestIds` | Must have completed those quests first |

---

## 12. World — Rooms, NPCs, Maps

### Navigation

```csharp
// Get adjacent rooms
var exits = player.CurrentRoom.Exits;  // Dictionary<string, Room>

// Move (update player state; add ticks if desired)
Room next = player.CurrentRoom.Exits["north"];
if (RoomService.CanEnterRoom(next, player))
{
    player.CurrentRoom   = next;
    player.CurrentRoomId = next.Id;
    DayCycleManager.AddTicks(GameTick.RoomTraversal);
}
```

### Spawning monsters

```csharp
// Dungeon rooms — populate on entry
player.CurrentRoom.SpawnDungeonMonsters();
var monsters = player.CurrentRoom.CurrentMonsters; // List<Monster>

// Random encounter from template pool
var monster = MonsterService.PickMonsterForFight(
    room.EncounterableMonsters,
    room.Monsters
);
```

### NPC interaction

```csharp
// Get NPCs in current room
var npcs = player.CurrentRoom.NpcRefs;  // List<Npc>

// Execute a service
NpcActionResult result = NpcInteractionService.Execute(
    player, npc,
    serviceId: "heal",   // "heal" | "buy_items" | "sell_items" | "upgrade" | "craft" | "talk"
    item: null,
    amount: 0
);

if (result.Success)
    Console.WriteLine(Localization.T(result.MessageKey, result.MessageArgs));
```

### Map rendering

```csharp
// Build a 2D map layout starting from the player's room
var layout = MapBuilder.BuildRoomMap(player.CurrentRoom);
// layout is a Dictionary<(int x, int y), Room> for grid rendering
```

---

## 13. Time Cycle & Ticks

The in-game clock advances by "ticks." Every `TicksPerSegment` ticks = one time segment. Four segments = one game day.

```
Morning → Midday → Evening → Night → (new day) → Morning → …
```

### Adding ticks

```csharp
DayCycleManager.AddTicks(GameTick.CombatVictory);  // 8 ticks
DayCycleManager.AddTicks(GameTick.Gather);          // 6 ticks
```

### Events

```csharp
DayCycleManager.SegmentChanged += (segment) =>
{
    // Update UI clocks, trigger day-specific spawns, etc.
};

DayCycleManager.DayAdvanced += (gameDay) =>
{
    // Apply daily penalties and ticks for all online players
    foreach (var player in onlinePlayers)
    {
        ClassManager.ApplyDailyPenalty(player);
        JobManager.ApplyDailyTicks(player, gameDay);
    }
};
```

### Inactivity timer

Slowly advances time while the player is idle.

```csharp
DayCycleManager.StartInactivityTimer(ticksPerInterval: 1, intervalMs: 10_000);
// adds 1 tick every 10 seconds
DayCycleManager.StopInactivityTimer();
```

### Current time

```csharp
TimeSegment current = DayCycleManager.CurrentTimeSegment;
int day = DayCycleManager.GameDay;
int ticks = DayCycleManager.CurrentTicks; // within current segment
```

---

## 14. Gathering & Crafting

### Gathering

```csharp
// Attempt a gather in the current room
GatherActionResult result = GatherService.Gather(player, player.CurrentRoom);

if (result.Success)
{
    Console.WriteLine($"Gathered {result.Amount}× {result.ItemId}");
    Console.WriteLine($"Skill XP gained: {result.SkillXpGained}");
    Console.WriteLine($"Gathers remaining today: {result.RemainingGathers}");
}
else
{
    // result.Reason: "no_spots" | "depleted" | "no_tool" | "inventory_full"
}
```

Gathering automatically:
- Checks the player has the correct tool for the spot's `GatheringType`
- Consumes a daily gather charge from the room
- Grants Skill XP for the active job (if any)
- Applies the skill multiplier (more items at higher skill levels)
- Adds ticks (`GameTick.Gather`)

### Daily gather limit

Each room rolls 1–5 daily gathers on day start. Knowledge level adds bonus charges:

```csharp
int bonus = JobManager.GetGatherKnowledgeBonus(player); // extra charges for today
```

### Crafting

Crafting is data-driven. The library provides the recipe registry; execution is in your UI:

```csharp
// List recipes for an NPC
var recipes = CraftingService.GetRecipes("master_smith");

// Get a specific recipe
var recipe = CraftingService.GetRecipe("master_smith", outputItemId: "iron_sword");

// Check ingredients (recipe.Ingredients is list of {ItemId, Amount})
bool hasAll = recipe.Ingredients.All(ing =>
    player.Inventory.Items.Where(i => i.Id == ing.ItemId).Sum(i => i.StackSize) >= ing.Amount
);

// If yes, remove ingredients, create item, add to inventory
```

---

## 15. Rune Magic

Rune magic is exclusive to the `RunicMage` class. Each rune starts from a base definition and gains power by adding runic words. Word pairs interact via Support, Contradiction, Neutral, or Transform relationships.

### Rune structure

```
BaseRune (template)
  └─ CompositeRune (player's instance)
       ├─ BaseRuneId  → BaseRuneData (stats, target, scaling)
       └─ AddedWordIds → List of RuneWord IDs
```

When words are added, `RuneEvaluator.Evaluate()` recomputes the skill's stats.

### Word pair effects

| Relationship | Effect |
|---|---|
| **Support** | Increases scaling factor |
| **Contradiction** | Also increases scaling (different flavor) |
| **Neutral** | Adds mana cost penalty |
| **Transform** | Unlocks a completely new rune (the transform result) |

Relationships are looked up in this priority order:
1. Explicit `WordPairRelation` entry for this exact pair
2. Family-level default (`WordFamily.FamilyRelations`)
3. Neutral (fallback)

### Rune management API

```csharp
// Add a word to a rune
bool ok = RuneManager.AddWord(player, rune, wordId, out var newRunes);
// newRunes: any runes unlocked via Transform (add these to player.KnownRunes)

// Remove a word
RuneManager.RemoveWord(player, rune, wordId);

// Player's translation dictionary
RuneManager.SetPlayerLabel(player, wordId, label: "Fire");   // user's guess
RuneManager.LearnWord(player, wordId);                        // officially learned (NPC/lore)

// Display a word as the player sees it
string display = RuneManager.GetDisplayName(player, wordId);
// → official name if learned, player's label if set, runic script otherwise
```

### Evaluating a rune

Called automatically when words change. You can call it manually for preview:

```csharp
Skill skill = RuneEvaluator.Evaluate(baseRune, addedWordIds);
```

---

## 16. Progression — XP & Levels

### Character XP

```csharp
// Grant XP (fires LeveledUp event if threshold crossed)
player.GainXp(500L);

// Inspect
int level = player.Level;
long xp    = player.Experience;
long next  = player.ExpForNextLvl;
```

Level-up grants stat points (tracked in `Stats.UnusedPoints`) and unlocks new skills.

### Class XP

```csharp
// Grant class XP
ClassManager.GrantClassXp(player, amount: 1000L);
ClassManager.GrantClassXp(player, PlayerClass.Knight, amount: 500L); // specific class

// Query
int classLevel = ClassManager.GetClassLevel(player, player.Class);
long classXp   = ClassManager.GetClassXp(player, player.Class);
string progress = ClassXpService.FormatProgress(classXp);

// Switch class (7-day cooldown, race restrictions enforced)
bool ok = ClassManager.SetClass(player, PlayerClass.Knight);
// Skills for old class are stashed; skills for new class are restored
// 50% XP transfer within the same ClassGroup

// Check allowed classes
IEnumerable<PlayerClass> allowed = ClassManager.GetAllowedClasses(player.Race);

// Daily penalty (call on DayAdvanced)
ClassManager.ApplyDailyPenalty(player);
```

**Class groups** (used for 50% XP transfer on in-group switch):
- `Physical` — Fighter, Knight, Barbarian
- `RangerRogue` — Archer, Hunter, Rogue
- `Mage` — ElementalMage, ArcanMage, RunicMage
- `DivineHybrid` — Cleric, SoulsKnight, Druid

### Job XP (see §10)

---

## 17. Authentication & Persistence

### User accounts

```csharp
// Register
LoginResult result = await LoginManager.Register("username", "password");

// Login
LoginResult result = await LoginManager.Login("username", "password");
if (result.Success)
{
    var account = result.Account; // UserAccount
    UserAccoundService.CurrentUser = account;
}
```

Passwords are hashed with PBKDF2-SHA512 (salt 16 bytes, hash 32 bytes, 200,000 iterations). Never stored in plain text.

### Character persistence

```csharp
// Save
await CharacterService.SaveCharacter(userAccount, player);

// Load
Player player = await CharacterService.LoadCharacter("HeroName", userAccount);

// List character names for account
string[] names = await characterRepository.GetNamesAsync("username");

// Delete
await characterRepository.DeleteAsync("username", "HeroName");
```

Files are stored at `Data/saves/{username}-{characterName}.json`. Accounts at `Data/users/{username}.json`.

### Settings

```csharp
SettingsService.Load();           // loads Data/Misc/settings.json
var settings = Settings.Current;

settings.LanguageSettings.Local = GameLanguage.German;
settings.VisualSettings.DarkMode = true;

SettingsService.Save();
```

---

## 18. Events Reference

### Player events

```csharp
player.XpGained     += (_, e) => { /* e.Amount, e.TotalXp, e.NextLevelXp */ };
player.LeveledUp    += (_, e) => { /* e.OldLevel, e.NewLevel */ };
player.HealthChanged += (_, e) => { /* e.OldHp, e.NewHp, e.Source */ };
player.ManaChanged  += (_, e) => { /* e.OldMana, e.NewMana, e.Source */ };
player.SkillLearned += (_, e) => { /* e.Skill */ };
```

### Inventory events

```csharp
player.Inventory.ItemReceived += (_, e) => { /* e.Item, e.StackSize, e.Source */ };
player.Inventory.ItemRemoved  += (_, e) => { };
player.Inventory.ItemSold     += (_, e) => { };
```

### Combat events

```csharp
encounter.MonsterKilled += (_, e) => { /* e.MonsterId */ };
```

### World events

```csharp
DayCycleManager.SegmentChanged += (segment) => { /* TimeSegment */ };
DayCycleManager.DayAdvanced    += (day) => { /* int */ };
```

### Game log

```csharp
GameLog.EntryAdded += (_, entry) =>
{
    if (entry.IsError)
        Console.Error.WriteLine(entry.Message);
    else
        Console.WriteLine(entry.Message);
};

// Recent entries (last 20)
foreach (var e in GameLog.RecentEntries) { }
```

---

## 19. Enum Registries

The simple enum registries (`EquipmentTypeRegistry`, `ItemRarityRegistry`, `TimeSegmentRegistry`, `GatheringTypeRegistry`) provide display names and ordering loaded from JSON. The C# enums remain the source of truth for game logic.

```csharp
// After GameConfig.LoadEquipmentSlots()
string name = EquipmentTypeRegistry.GetDisplayName(EquipmentType.Weapon); // "Weapon Slot"
var allSlots = EquipmentTypeRegistry.All; // IReadOnlyList<EnumDefinition>

// ItemRarityRegistry also has ordering
int order = ItemRarityRegistry.GetOrder(ItemRarity.Legendary); // e.g. 5
string rareName = ItemRarityRegistry.GetDisplayName(ItemRarity.Legendary);

// Same pattern for Time and Gathering
string segName = TimeSegmentRegistry.GetDisplayName(TimeSegment.Morning);
string typeName = GatheringTypeRegistry.GetDisplayName(GatheringType.Ore);
```

Race and class profiles are accessed via:

```csharp
// After GameConfig.LoadRaces() / GameConfig.LoadClasses()
if (RaceProfile.All.TryGetValue(player.Race, out var profile))
{
    int hpGrowth = profile.HpPerLevel;
    var forbidden = profile.ForbiddenClasses; // HashSet<PlayerClass>
}

if (ClassProfile.All.TryGetValue(player.Class, out var profile))
{
    int statGain = profile.StatGrowth["STR"];
}
```

---

## 20. Localization

```csharp
// Load at startup (already called by GameService.InitializeGame)
Localization.Load(GameLanguage.English);

// Translate a key
string text = Localization.T("npc.healer_mira.name");

// With format arguments
string text = Localization.T("combat.dealt_damage", playerName, damageAmount);
```

Locale files live at `Data/locales/en.json` and `Data/locales/de.json`. Format is a flat JSON object:
```json
{
  "npc.healer_mira.name": "Healer Mira",
  "combat.dealt_damage":  "{0} deals {1} damage."
}
```

---

## 21. Mod System

The mod system lives in `MyriaLib.Systems.Mods` and allows any consuming application to overlay custom game data on top of the base JSON files without touching source code. Visual assets (icons, images, locale strings) are always applied; gameplay data (items, monsters, rooms, etc.) is automatically suppressed when the client connects to a multiplayer server.

### How mods work

A mod is a folder placed inside a `Mods/` directory next to the game executable. Each mod folder must contain a `mod.json` manifest and may contain any number of data-file overrides that mirror the base `Data/` tree.

```
Mods/
├── dark_icons/           ← visual-only mod
│   ├── mod.json
│   └── Data/
│       └── Icons/
│           └── iron_ore.svg
└── harder_monsters/      ← gameplay mod
    ├── mod.json
    └── Data/
        └── common/
            └── monsters.json
```

### `mod.json` schema

```json
{
  "id":          "harder_monsters",
  "name":        "Harder Monsters",
  "version":     "1.0.0",
  "author":      "YourName",
  "description": "Increases monster stats across the board.",
  "enabled":     true,
  "loadOrder":   100,
  "settings": [
    {
      "key":          "accentColor",
      "label":        "Accent Color",
      "description":  "Theme accent used by this visual mod.",
      "type":         "colorSlider",
      "defaultValue": "#C83232"
    },
    {
      "key":          "accentBrightness",
      "label":        "Accent Brightness",
      "description":  "Scales derived theme colors.",
      "type":         "slider",
      "defaultValue": "100",
      "min":          40,
      "max":          160,
      "step":         5
    },
    {
      "key":          "animateAccent",
      "label":        "Animate Accent",
      "description":  "Cycles the accent color over time.",
      "type":         "bool",
      "defaultValue": "false"
    }
  ],
  "settingValues": {
    "accentColor": "#C83232",
    "accentBrightness": "100",
    "animateAccent": "false"
  },
  "visualEffects": [
    {
      "key":               "accentHueCycle",
      "target":            "themeAccentPalette",
      "effect":            "hueCycle",
      "sourceSetting":     "accentColor",
      "brightnessSetting": "accentBrightness",
      "enabledSetting":    "animateAccent",
      "speed":             0.2
    }
  ]
}
```

| Field | Default | Description |
|---|---|---|
| `id` | folder name | Unique identifier used for multiplayer validation |
| `name` | — | Display name |
| `version` | `"1.0.0"` | Shown in mod lists |
| `author` | — | Optional |
| `description` | — | Optional |
| `enabled` | `true` | Disabled mods are visible in settings but do not apply overrides |
| `loadOrder` | `100` | Lower = loaded first; higher-order mods win conflicts |
| `settings` | `[]` | Optional mod-specific setting definitions shown by supporting frontends |
| `settingValues` | `{}` | Persisted values keyed by setting key |
| `visualEffects` | `[]` | Optional runtime visual effect definitions interpreted by supporting frontends |

Supported generic setting types are `text`, `number`, `color`, `colorSlider`, `slider`, and `bool`. `colorSlider` renders a hue slider while still saving a hex color string such as `#C83232`. Slider definitions can set `min`, `max`, and `step`. Frontends can render these as native controls and extenders can read the resolved values from `LoadedMod.Manifest.SettingValues`.

Visual effects are declarative recipes. `MyriaLib` only parses them; UI projects decide whether and how to run them. A WPF host might support `target: "themeAccentPalette"` with `effect: "hueCycle"`, while another frontend can ignore unknown targets or effects.

### Visual vs gameplay classification

Classification is automatic — no flag needed in `mod.json`.

| Path prefix | Classification |
|---|---|
| `Data/locales/` | Visual |
| `Data/Icons/` | Visual |
| `Data/images/` | Visual |
| `Data/Maps/` | Visual |
| `Assets/` | Visual |
| `Data/common/` | **Gameplay** |

A mod is marked **visual-only** if every file it contains falls under a visual prefix. One gameplay file anywhere in the mod makes the whole mod gameplay-affecting.

### Override strategy

The last mod in load order that provides a file wins for that file. A mod that overrides `monsters.json` must ship the complete file — partial merging is not supported. Use `loadOrder` to ensure your mod applies after any others it depends on.

### Loading mods in code

```csharp
using MyriaLib.Systems.Mods;

// Call before GameService.InitializeGame()
ModLoader.Load("Mods");   // silently returns if Mods/ does not exist

// Inspect what was loaded
foreach (var mod in ModLoader.ActiveMods)
    Console.WriteLine($"{mod.Manifest.Name} v{mod.Manifest.Version} — visual-only: {mod.IsVisualOnly}");

// Initialize game — all data paths automatically resolved through mod overrides
GameService.InitializeGame();
```

### Multiplayer mode

When the player connects to a server, gameplay mods must be suppressed so the client uses the same unmodified data the server expects.

```csharp
// On multiplayer connect
ModLoader.MultiplayerMode = true;
GameService.InitializeGame();    // reloads everything; gameplay mods are skipped

// On disconnect
ModLoader.MultiplayerMode = false;
GameService.InitializeGame();    // restores mod overrides for singleplayer
```

`ModLoader.ResolvePath(defaultPath)` is the single chokepoint that all loaders call. When `MultiplayerMode` is `true`, it skips gameplay mods and returns the unmodded path.

### Server mod validation

Clients send their mod list to the server immediately after connecting so the server can audit and warn about gameplay mods.

```csharp
// Client side (GameHubService does this automatically)
ModInfo info = ModLoader.GetModInfo();
await hubConnection.InvokeAsync("ReportMods", info);

// Server responds with "ModValidation" event: (bool accepted, string message)
// accepted = true  → no gameplay mods, or visual-only → ok
// accepted = false → gameplay mods detected; client should suppress them
```

`ModInfo` contains:
- `ActiveMods` — list of `ModEntry` (id, name, version, isVisualOnly, fingerprint)
- `HasGameplayMods` — computed convenience property

Each gameplay mod carries a `Fingerprint` — a SHA-256 hash of all its gameplay-affecting files. The server can compare fingerprints against an approved allowlist if stricter enforcement is needed.

### `ModLoader` API reference

| Member | Description |
|---|---|
| `ModLoader.Load(string dir = "Mods")` | Scan directory, register mods sorted by LoadOrder |
| `ModLoader.ActiveMods` | `IReadOnlyList<LoadedMod>` — enabled mods that currently apply |
| `ModLoader.AllMods` | `IReadOnlyList<LoadedMod>` — all discovered mods, including disabled mods |
| `ModLoader.VisualMods` | Subset of `ActiveMods` where `IsVisualOnly == true` |
| `ModLoader.GameplayMods` | Subset of `ActiveMods` where `IsVisualOnly == false` |
| `ModLoader.MultiplayerMode` | `bool` — when true, gameplay overrides are bypassed in `ResolvePath` |
| `ModLoader.ResolvePath(string path)` | Returns the effective file path after applying mod overrides |
| `ModLoader.RegisterExtender(IModLoaderExtender)` | Register a project-specific hook for applying UI/visual mod behavior |
| `ModLoader.GetModInfo()` | Builds a `ModInfo` snapshot for server validation |

### Mod loader extenders

`MyriaLib` stays UI-independent: it can classify visual mods and resolve file paths, but it does not know how a WPF, console, web, or other frontend should apply visual assets.

Frontend projects can register an extender before calling `ModLoader.Load()`:

```csharp
public sealed class WpfVisualModExtender : ModLoaderExtender
{
    public override void BeforeModsReload(ModLoadContext context)
    {
        // Remove previously loaded resource dictionaries or clear image caches.
    }

    public override void AfterModsLoaded(ModLoadContext context)
    {
        foreach (var mod in context.VisualMods)
        {
            // Apply project-specific visual assets from mod.Directory.
        }
    }
}

ModLoader.RegisterExtender(new WpfVisualModExtender());
ModLoader.Load("Data/Mods");
```

If an extender throws, mod loading continues and the failure is stored in `ModLoader.ExtenderErrors` for the host project to display or log.

### `LoadedMod` properties

| Property | Description |
|---|---|
| `Manifest` | The parsed `mod.json` content |
| `Directory` | Absolute path to the mod's root folder |
| `IsVisualOnly` | True if every file in the mod is under a visual prefix |
| `OverriddenFiles` | List of relative paths this mod provides |
| `Fingerprint` | SHA-256 of all gameplay files; empty string for visual-only mods |

---

*MyriaLib — all systems, one library.*
