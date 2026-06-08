Item:

	ConsumableItem:
		HealAmount is an integer value that determines how much health the item restores when used.
		ManaRestore is an integer value that determines how much mana the item restores when used.

		Use() is a method that applies the effects of the consumable item to the player. It increases the player's health by the HealAmount and restores their mana by the ManaRestore value. After using the item, it is removed from the player's inventory.

	Currency:
		is an WPF only class used to display money values in the UI. It uses a format string to determine the format of the displayed money value. The default format is "S", which stands for "Silver". The Convert method takes a Money object and converts it to a string based on the specified format and culture. The ConvertBack method is not supported and will throw an exception if called.

	EquipmentBonuses:
		Saves all bonuses that an equipment item has. This includes: Hp, Mp, STR, DEX, INT, SPR, ATK, DEF, MATK, MDEF, Aim, Evasion, Crit and Block.

		Scale() is a method that scales the bonuses based on the upgrade level of the equipment and returns a copy.

	EquipmentItem:
		A class for Equipment items that extends the Item class.

		EquipmentType is an enum that defines the type of equipment (e.g., Helmet, Armor, Weapon, etc.).
		BuyPrice is an integer value that determines the cost of purchasing the equipment item from a shop.
		BaseStats is an instance of the EquipmentBonuses class that holds the base bonuses provided by the equipment item.
		Bonuses is an instance of the EquipmentBonuses class that holds the current bonuses provided by the equipment item, which may be scaled based on upgrades.
		UpgradeLevel is an integer value that represents the current upgrade level of the equipment item. Upgrading the item increases its bonuses.
		UpgradeCategory is an string value that categorizes the type of upgrade the EquipmentItem can recieve.
		CraftQuantity is an float value that determines how many of the items are produced when crafting this item.
		Bonus-Hp - Evasion are all integer values that represent the bonuses provided by the equipment item.
		BonusCrit, BonusBlock are float values that represent the critical hit chance and block chance bonuses provided by the equipment item, respectively.

		IsUsableBy() is a method that checks if the equipment item can be used by a player based on their class.
		Use() is a method required by Item, it is not used for equipment items and quits if called.
		TryUpgrade() is a method that attempts to upgrade the equipment item. It checks if the item will reach a higher upgrade level than it's allowed maximum upgrade level, and if not calles TryUpgrade_internal() to scale the Bonuses.
		TryUpgrade_internal() is a method that scales the Bonuses of the equipment item based on its UpgradeLevel and updates the Bonus-Hp - Evasion values accordingly. It also calles the Scale() method of the EquipmentBonuses class.

	InventoryExpansion:
		Is a class for inventory expansion items that extends the Item class. It is used to increase the player's inventory capacity.

		MaxStackSize is an integer value inherited from Item that determines how many items of this type can be stacked in a single inventory slot, the base value is 1.

		Use() is a method inherited by Item, it is used here to increase the player's inventory capacity by a certain amount when the item is used.

	Item:
		Base class for all items in the game.

		Id is a unique identifier for the item, it is a string value.
		Name is the name of the item, it is a string value.
		Description is a string value that provides a description of the item.
		Rarity is an enum value that represents the rarity of the item (e.g., Common, Uncommon, Rare, Epic, Legendary).
		StackSize is an integer value that determines how many items of this type are currently stacked in a single inventory slot.
		MaxStackSize is an integer value that determines how many items of this type can be stacked in a single inventory slot.
		IsTool is a boolean value that indicates whether the item is a tool or not.
		ToolType is an enum value that represents the type of tool (e.g., Pickaxe, Axe, Shovel) if the item is a tool.
		BuyPrice is an integer value that determines the cost of purchasing the item from a shop.
		SellValue is an integer value that determines the amount of money the player receives when selling the item to a shop. Its base value is 75% of the BuyPrice.
		JobId is an string value that indicates which job is connected to the item, it is used for crafting, quests and job leveling.

		Use() is a method that defines the behavior of the item when used by the player. The specific implementation of this method will depend on the type of item and its intended functionality. For example, using a consumable item might restore health or mana, while using a tool might allow the player to perform certain actions in the game world.
		CanStackWith() is a method that checks if the current item can be stacked with another item. It compares the Id of the two items and returns true if they are the same, indicating that they can be stacked together. If the Ids are different, it returns false, indicating that the items cannot be stacked together.
		CloneOne() is a method that creates a new instance of the item with the same properties as the original item, but with a StackSize of 1. This is useful for splitting stacks of items or creating individual items from a stack. The method returns the new instance of the item.

	MaterialItem:
		Is a class for material items that extends the Item class. It is used for crafting and other purposes in the game.

		MaxStackSize is an integer value inherited from Item that determines how many items of this type can be stacked in a single inventory slot, the base value is 99.

		Use() is a method inherited by Item, it is not used for material items and quits if called.

	UniqueLootEntry:
		Is a class for unique loot entries that references the Item class's Id. It is used to define unique loot drops in the game.

		ItemId is a string value that references the Id of an item in the Item class. This allows the UniqueLootEntry to specify which item is being referenced for the unique loot drop. The UniqueLootEntry can be used in loot tables or other systems to define specific items that can be obtained as unique drops in the game.
		DropChance is a float value that represents the chance of the unique loot entry dropping when it is included in a loot table or drop system. This value is typically expressed as a percentage (e.g., 0.25 for a 25% drop chance) and is used to determine the likelihood of the item being obtained by players when they defeat enemies, open chests, or engage in other activities that can yield loot.


Job:
	Job:
		Is a class that represents a player's job or profession in the game. It contains information about the job's name, description, level, experience points, and the items that are associated with the job.

		Id is a unique identifier for the job, it is a string value.
		Name is the name of the job, it is a string value.
		Description is a string value that provides a description of the job.
		Type is a string value that represents the type of job (e.g., Smith, Alchemist, Miner).

	PlayerJob:
		Is a class that represents a player's job or profession in the game. It contains information about the player's job-specific attributes and abilities.

		JobId is a string value that references the Id of a Job, indicating which job the player has chosen.
		SkillXp is a long value that represents the player's current experience points in their chosen job. As the player gains experience points through various activities related to their job, this value increases, allowing them to level up and unlock new abilities or bonuses associated with their job.
		KnowledgeXp is a long value that represents the player's current knowledge experience points in their chosen job. Similar to SkillXp, as the player gains knowledge experience points through activities related to their job, this value increases, contributing to the player's overall progression and mastery of their chosen profession.
		FameXp is a long value that represents the player's current fame experience points in their chosen job. FameXp is typically earned through notable achievements, completing significant quests, or reaching milestones within the job. As the player accumulates fame experience points, they may gain recognition, unlock special rewards, or access exclusive content related to their profession.
		LastFameTickDay is an integer value that represents the last day on which the player received fame experience points for their job. This value is used to track when the player last earned fame experience points and can be used to implement daily or periodic fame rewards based on the player's activities within their chosen profession.
		LastSkillUsedDay is an integer value that represents the last day on which the player used a skill related to their job. This value is used to track when the player last utilized their job-specific skills and can be used to implement cooldowns, daily skill usage limits, or other mechanics that encourage players to engage with their chosen profession regularly.

Map:
	Cave:
		is an empty class inheriting from MapArea, it is used to define cave areas in the game. It can be expanded in the future to include specific properties or methods related to cave environments, such as unique enemies, resources, or environmental hazards that players may encounter while exploring caves in the game world.

	City:
		is a class inheriting from MapArea, it is used to define city areas in the game. It can be expanded in the future to include specific properties or methods related to city environments, such as unique NPCs, buildings, or social interactions that players may encounter while exploring cities in the game world.

		IsBig is a boolean value that indicates whether the city is considered a "big" city or not. This property is used to determine if a city should be printed out fully or as a MapNode.

	Dungeon:
		is an empty class inheriting from MapArea, it is used to define dungeon areas in the game. It can be expanded in the future to include specific properties or methods related to dungeon environments, such as unique enemies, traps, or puzzles that players may encounter while exploring dungeons in the game world.

	Forest:
		is an empty class inheriting from MapArea, it is used to define forest areas in the game. It can be expanded in the future to include specific properties or methods related to forest environments, such as unique flora and fauna, resources, or environmental hazards that players may encounter while exploring forests in the game world.

	GatheringSpot:
		is a class to save information about gathering spots in the game.

		Id is a unique identifier for the gathering spot, it is a string value.
		Name is the name of the gathering spot, it is a string value.
		Description is a string value that provides a description of the gathering spot.
		Type is an enum value that represents the type of gathering spot (e.g., Mining, Logging, Herbalism).
		GatheredItemId is a string value that references the Id of an item in the Item class, indicating the item that can be gathered from this spot.
		RequiredToolId is an enum value that represents the type of tool required to gather resources from the gathering spot (e.g., Pickaxe, Axe, Shovel).

	MapArea:
		is an abstract base class for defining different types of areas in the game world. It provides common properties and methods that can be inherited by specific map area types, such as Cave, City, Dungeon, and Forest.

		Id is a unique identifier for the map area, it is a string value.
		Name is the name of the map area, it is a string value.
		RoomIds is a list of int values that represent the unique identifiers of rooms that are part of this map area. Each room in the game world can be associated with a specific map area, and the RoomIds property allows for easy reference and organization of rooms within their respective areas. This structure helps to manage the game's environment and facilitates navigation and interaction for players as they explore different locations in the game world.
		AnchorRoomId is an integer value that represents the unique identifier of a specific room within the map area that serves as an anchor point. This anchor room can be used as a reference point for various purposes, such as spawning NPCs, placing important objects, or serving as a central location for quests or events within the map area. The AnchorRoomId helps to establish a focal point within the map area and can be utilized in game mechanics and design to enhance player experience and engagement.
		MapFile is a string value that represents the file path or name of the map data associated with this map area. This property can be used to load specific map layouts, textures, or other relevant data when players enter or interact with the map area, allowing for a more immersive and visually distinct environment within the game world. It is mainly used for Console.

		ContainsRoom() is a method that checks if a given room ID is part of the map area by verifying if it exists in the RoomIds list. This method can be used to determine if a player is currently within the boundaries of the map area or to trigger specific events or interactions based on the player's location within the game world.

	Room:
		is a class that represents a single room within the game world. It contains properties and methods for managing the room's state, such as its location, connections to other rooms, and the objects or characters present within it.

		Id is a unique identifier for the room, it is an integer value.
		Name is the name of the room, it is a string value.
		Description is a string value that provides a description of the room.
		HasMonsters is a boolean value that indicates whether the room contains monsters or not. This property can be used to determine if players will encounter combat when entering the room, and it can also influence the types of interactions and events that may occur within the room, such as loot drops, quests, or environmental hazards. The HasMonsters property helps to create a dynamic and engaging game world by adding variety and challenge to different rooms based on their content.
		RequirementType is an enum value that represents the type of requirement needed to access the room (e.g., Key, Puzzle, Combat). This property can be used to determine what players need to do in order to enter the room, adding an additional layer of gameplay and exploration as players must meet certain conditions or overcome challenges to progress through the game world. The RequirementType helps to create a more immersive and interactive experience for players as they navigate through different rooms and areas in the game.
		AccessLevel is an integer value that represents the required access level for players to enter the room. This property can be used to gate certain areas of the game world, ensuring that players have reached a certain point in their progression or have acquired specific items or abilities before they can access more challenging or rewarding content. The AccessLevel helps to create a sense of progression and accomplishment for players as they advance through the game and unlock new areas to explore.
		RequiredQuestId is a string value that references the Id of a quest in the Quest class, indicating
		IsDungeon is a boolean value that indicates whether the room is part of a dungeon or not. This property can be used to differentiate between regular rooms and those that are specifically designed as part of a dungeon environment, which may have unique challenges, enemies, or rewards associated with them. The IsDungeon property helps to create a more immersive and varied game world by allowing for distinct types of rooms and experiences based on their context within the game's environment.
		IsBossRoom is a boolean value that indicates whether the room is a boss room or not. This property can be used to identify rooms that contain powerful enemies or bosses, which may require special strategies, equipment, or teamwork to defeat. The IsBossRoom property helps to create memorable and challenging encounters for players, adding excitement and a sense of accomplishment when they successfully overcome these formidable foes within the game world.
		IsCaveRoom is a boolean value that indicates whether the room is part of a cave environment or not. This property can be used to differentiate between rooms that are designed with cave-like features, such as narrow passages, stalactites, or underground settings, and those that are part of other types of environments. The IsCaveRoom property helps to create a more immersive and visually distinct game world by allowing for varied room designs and atmospheres based on their environmental context.
		IsCity is a boolean value that indicates whether the room is part of a city environment or not. This property can be used to differentiate between rooms that are designed with urban features, such as buildings, streets, or marketplaces, and those that are part of other types of environments. The IsCity property helps to create a more immersive and visually distinct game world by allowing for varied room designs and atmospheres based on their environmental context.
		IsCleared is a boolean value that indicates whether the room has been cleared of monsters or not. This property can be used to track the player's progress through the game world, as well as to trigger specific events or interactions based on whether the room has been cleared. For example, clearing a room may unlock new areas, provide access to hidden treasures, or allow players to complete quests that require them to defeat all enemies in a specific location. The IsCleared property helps to create a sense of accomplishment and progression for players as they explore and conquer different rooms within the game world.
		DailyGatherLimit is an integer value that represents the maximum number of times players can gather resources from the room on a daily basis. This property can be used to implement resource management mechanics, encouraging players to strategize their gathering activities and return to the room on subsequent days to gather more resources. The DailyGatherLimit helps to create a more dynamic and engaging game world by adding a layer of resource management and planning for players as they explore different rooms and environments.
		GathersRemaining is an integer value that represents the number of gathers remaining for the day in the room. This property is used in conjunction with the DailyGatherLimit to track how many times players have gathered resources from the room and how many gathers they have left for the day. The GathersRemaining property helps to create a more immersive and interactive game world by providing players with real-time feedback on their resource gathering activities and encouraging them to manage their gathering efforts strategically as they explore different rooms and environments.
		GatheringSpots is a list of GatheringSpot objects that represent the specific locations within the room where players can gather resources. Each GatheringSpot contains information about the type of gathering activity (e.g., Mining, Logging, Herbalism), the item that can be gathered, and the required tool for gathering. The GatheringSpots property helps to create a more immersive and interactive game world by providing players with specific locations and opportunities for resource gathering within each room, encouraging exploration and strategic planning as they navigate through different environments.
		Npcs is a list of NPC string values that represent the unique identifiers of non-player characters (NPCs) present in the room. This property can be used to track which NPCs are located in each room, allowing for interactions such as quests, trading, or dialogue. The Npcs property helps to create a more dynamic and engaging game world by populating rooms with characters that players can interact with, adding depth and immersion to the overall gaming experience.
		ExitIds is a dictionary that maps exit directions (e.g., North, South, East, West) to the unique identifiers of the rooms that can be accessed through those exits. This property is used to define the connections between rooms in the game world, allowing players to navigate from one room to another based on the available exits. The ExitIds property helps to create a cohesive and interconnected game world by establishing clear pathways for players to explore and discover new areas as they progress through the game.
		Exits is a dictionary that maps exit directions (e.g., North, South, East, West) to the actual Room objects that can be accessed through those exits. This property is used to facilitate navigation between rooms in the game world, allowing players to move from one room to another based on the available exits. The Exits property helps to create a more immersive and interactive game world by providing players with clear pathways for exploration and discovery as they navigate through different environments and encounter various challenges along the way.
		EncounterableMonsters is a dictionary that maps monster identifiers to their corresponding encounter details, such as spawn rates, locations, and conditions for encountering them within the room. This property is used to define the various monsters that players may encounter while exploring the room, adding an element of challenge and excitement to the gameplay experience. The EncounterableMonsters property helps to create a more dynamic and engaging game world by populating rooms with a variety of monsters that players can interact with, fight against, or avoid as they navigate through different environments and progress through the game.
		Monsters is a list of Monster objects that represent the specific monsters currently present in the room. This property is used to track the monsters that players may encounter while exploring the room, allowing for interactions such as combat, loot drops, or quest objectives. The Monsters property helps to create a more dynamic and engaging game world by populating rooms with a variety of monsters that players can interact with, fight against, or avoid as they navigate through different environments and progress through the game.
		Corpses is a list of Corpse objects that represent the remains of defeated monsters or NPCs within the room. This property can be used to track the aftermath of combat encounters, allowing players to loot corpses for items, gather resources, or complete quests that require them to interact with the remains of fallen characters. The Corpses property helps to create a more immersive and interactive game world by providing players with tangible consequences for their actions and adding depth to the overall gaming experience as they explore different rooms and environments.
		NpcRefs is a list of NPCRef objects that represent references to non-player characters (NPCs) present in the room. Each NPCRef contains information about the NPC's unique identifier, location within the room, and any relevant interactions or quests associated with that NPC. The NpcRefs property helps to create a more dynamic and engaging game world by providing players with specific characters to interact with, adding depth and immersion to the overall gaming experience as they explore different rooms and environments.

		Room() is the constructor for the Room class and only allowes rooms with at least a name and description to be created, all other properties are optional and can be set after the room is created. This constructor ensures that every room has a basic level of information, such as its name and description, which are essential for players to understand the context and atmosphere of the room as they explore the game world. By allowing other properties to be set after the room is created, it provides flexibility for game designers to customize each room with unique features, challenges, and interactions based on their specific design goals and the overall narrative of the game.

		ConnectRoom() is a method that establishes a connection between the current room and another room in a specified direction. This method takes in the direction (e.g., North, South, East, West) and the target room as parameters, and it updates the ExitIds and Exits properties of both rooms to reflect the new connection. By using this method, game designers can easily create a cohesive and interconnected game world, allowing players to navigate seamlessly between different rooms and explore the various environments and challenges that the game has to offer.
		RollGatherLimit() is a method that randomly determines the number of gathers available for the day in the room based on the DailyGatherLimit property. This method can be called at the start of each in-game day to reset the GathersRemaining property, providing players with a fresh set of gathering opportunities as they explore the room and its resources. The RollGatherLimit method helps to create a more dynamic and engaging game world by adding an element of randomness to resource gathering, encouraging players to return to rooms on subsequent days to take advantage of new gathering opportunities and manage their resources strategically.
		AddGatherBonus() is a method that adds a bonus to the gathering limit in the room based on certain conditions, such as player skills, equipment, or buffs. This method can be called when players enter the room or when they activate specific abilities that enhance their gathering capabilities. By using the AddGatherBonus method, game designers can create a more dynamic and interactive game world, allowing players to influence their resource gathering opportunities through their actions and choices, and encouraging them to invest in skills and equipment that enhance their gathering potential.
		SpawnDungeonMonsters() is a method that populates the room with monsters based on the EncounterableMonsters property. This method can be called when players enter a dungeon room or when specific conditions are met, such as time of day or player level. By using the SpawnDungeonMonsters method, game designers can create a more dynamic and engaging game world, providing players with varied combat encounters and challenges as they explore different rooms and environments within dungeons.
		TryConsumeGather() is a method that attempts to consume a gather from the room when a player tries to gather resources. This method checks if there are any gathers remaining for the day (GathersRemaining > 0) and if the player meets any necessary conditions for gathering (e.g., having the required tool or skill level). If the gather is successfully consumed, the method decrements the GathersRemaining property and allows the player to gather resources from the room. By using the TryConsumeGather method, game designers can create a more immersive and interactive game world, encouraging players to manage their gathering activities strategically and providing a sense of consequence for their actions as they explore different rooms and environments.

Monster:
	Corpse:
		Corpse is a class that represents the remains of a defeated monster or NPC within the game world. It contains properties and methods for managing the corpse's state, such as its location, the items it contains, and the interactions players can have with it.

		Name is the name of the corpse, it is a string value.
		Loot is a list of Item objects that represent the items contained within the corpse. This property can be used to track the loot that players can obtain by interacting with the corpse, such as looting defeated monsters for valuable items, resources, or quest-related objects. The Loot property helps to create a more immersive and rewarding game world by providing players with tangible rewards for their combat encounters and encouraging them to explore and interact with the remains of fallen characters as they progress through the game.
		IsLooted is a boolean value that indicates whether the corpse has been looted by players or not. This property can be used to track the state of the corpse and prevent players from looting it multiple times, ensuring that the loot contained within the corpse is only obtained once. The IsLooted property helps to create a more immersive and interactive game world by adding a sense of consequence to player actions, encouraging them to explore and interact with corpses while also managing their expectations regarding loot availability as they progress through the game.

		Corpse() is the constructor for the Corpse class and requires a name and a list of loot items to be created. This constructor ensures that every corpse has a basic level of information, such as its name and the loot it contains, which are essential for players to understand the context and rewards associated with the corpse as they interact with it in the game world. By providing a structured way to manage corpses and their loot, the Corpse class helps to create a more immersive and rewarding gaming experience for players as they explore different rooms and environments within the game.

	Monster:
		Monster is a class inhereting from CombatEntity that represents a specific type of monster in the game world. It contains properties and methods for managing the monster's state, such as its health, attack power, and the loot it drops when defeated.

		Id is a unique identifier for the monster, it is a integer value.
		Description is a string value that provides a description of the monster, including its appearance, behavior, and any notable characteristics that players may find interesting or useful when encountering the monster in the game world.
		Type is an enum value that represents the type of monster (e.g., Goblin, Skeleton, Dragon). This property can be used to categorize monsters based on their species or classification, allowing for easier organization and reference within the game world. The Type property helps to create a more immersive and engaging game world by providing players with a variety of monsters to encounter, each with their own unique traits and behaviors based on their type.
		Level is an integer value that represents the monster's level, which can be used to determine its strength, health, and the difficulty of defeating it. The Level property helps to create a more dynamic and engaging game world by providing players with a range of monsters to encounter, each with varying levels of challenge based on their level.
		Exp is an integer value that represents the amount of experience points (XP) players can earn by defeating the monster. This property can be used to reward players for their combat encounters and encourage them to engage with a variety of monsters as they progress through the game. The Exp property helps to create a more immersive and rewarding game world by providing players with tangible rewards for their efforts in defeating monsters and advancing their character's progression.
		MinLoot is an integer value that represents the minimum amount of loot players can expect to receive when defeating the monster. This property can be used to set a baseline for the rewards players can obtain from combat encounters, providing a sense of consistency and predictability in the loot system. The MinLoot property helps to create a more immersive and rewarding game world by giving players an idea of what they can expect to gain from defeating monsters, encouraging them to engage with a variety of monsters as they progress through the game.
		MaxLoot is an integer value that represents the maximum amount of loot players can receive when defeating the monster. This property can be used to set an upper limit on the rewards players can obtain from combat encounters, adding an element of excitement and variability to the loot system. The MaxLoot property helps to create a more immersive and rewarding game world by providing players with the potential for greater rewards when defeating monsters, encouraging them to engage with a variety of monsters as they progress through the game.
		LootTable is a list of Item objects that represent the specific items that can be obtained as loot when defeating the monster. Each UniqueLootEntry contains information about the item being referenced and its drop chance, allowing for a more detailed and varied loot system. The LootTable property helps to create a more immersive and rewarding game world by providing players with specific items to look forward to when defeating monsters, adding depth and excitement to combat encounters as they progress through the game.
		UniqueLootTable is a list of UniqueLootEntry objects that represent the specific items that can be obtained as unique loot when defeating the monster. Each UniqueLootEntry contains information about the item being referenced and its drop chance, allowing for a more detailed and varied loot system. The UniqueLootTable property helps to create a more immersive and rewarding game world by providing players with specific items to look forward to when defeating monsters, adding depth and excitement to combat encounters as they progress through the game.
		DropsCorpse is a boolean value that indicates whether the monster drops a corpse when defeated or not. This property can be used to determine if players will have the opportunity to interact with the remains of the monster after defeating it, such as looting the corpse for additional items or resources. The DropsCorpse property helps to create a more immersive and interactive game world by providing players with tangible consequences for their combat encounters and adding depth to the overall gaming experience as they explore different rooms and environments within the game.

		Monster() is the constructor for the Monster class and requires an Id, name, stats (from the Stats class), description and exp value to be created.

		ResetHealth() is a method that resets the monster's health to its maximum value. This method can be called when the monster is respawned or when it is healed through certain abilities or interactions. By using the ResetHealth method, game designers can create a more dynamic and engaging game world, allowing monsters to recover and continue to pose a challenge to players as they explore different rooms and environments within the game.
		Clone() is a method that creates a new instance of the monster with the same properties as the original monster. This method can be used to spawn multiple instances of the same monster in different rooms or at different times, allowing for a more dynamic and engaging game world. By using the Clone method, game designers can easily populate their game world with a variety of monsters while maintaining consistency in their properties and behaviors, enhancing the overall gaming experience for players as they encounter different monsters throughout their journey.

NPC:
	DialogLine:
		This class is fully there for dialogs for quests

		Speaker is a string value that represents the name of the character or entity that is speaking the dialog line. This property can be used to identify who is delivering the dialog, providing context and immersion for players as they engage with the game's narrative and characters.
		Text is a string value that represents the actual content of the dialog line being spoken by the speaker. This property can be used to convey information, advance the story, or provide flavor and personality to the characters in the game, enhancing the overall narrative experience for players as they interact with NPCs and engage with quests.

	Npc:
		



