using MyriaLib.Entities.Characters;
using MyriaLib.Entities.Items;
using MyriaLib.Entities.Maps;
using MyriaLib.Entities.Monsters;

namespace MyriaLib.Systems
{
    /// <summary>
    /// Central publish/subscribe hub for game-lifecycle events.
    /// <para>
    /// <b>For mod authors (DLL mods):</b> subscribe to these events inside your
    /// <see cref="MyriaLib.Systems.Mods.IModLoaderExtender.AfterModsLoaded"/> implementation.
    /// Unsubscribing is not required — the game unloads plugin assemblies between sessions.
    /// </para>
    /// <para>
    /// <b>For game code:</b> fire via the static Invoke calls; all events are null-safe.
    /// </para>
    /// </summary>
    public static class GameEvents
    {
        // ── Session ───────────────────────────────────────────────────────────

        /// <summary>Fires when a character session becomes active (entering the game world).</summary>
        public static event Action<Character>? SessionStarted;

        // ── Progression ───────────────────────────────────────────────────────

        /// <summary>Fires after a character levels up. <c>newLevel</c> is the level just reached.</summary>
        public static event Action<Character, int>? LevelUp;

        /// <summary>Fires after a character changes their active class.</summary>
        public static event Action<Character, string, string>? ClassChanged;

        // ── World ─────────────────────────────────────────────────────────────

        /// <summary>Fires after a character enters a room.</summary>
        public static event Action<Character, Room>? RoomEntered;

        /// <summary>Fires when the in-game day advances. <c>day</c> is the new day number.</summary>
        public static event Action<int>? DayAdvanced;

        // ── Combat ────────────────────────────────────────────────────────────

        /// <summary>Fires after a character kills a monster in solo combat.</summary>
        public static event Action<Character, Monster>? MonsterKilled;

        // ── Items ─────────────────────────────────────────────────────────────

        /// <summary>Fires after a consumable item is used successfully.</summary>
        public static event Action<Character, Item>? ItemUsed;

        /// <summary>
        /// Fires after an item is added to a character's inventory (gather, loot, purchase, quest
        /// reward, etc.). <c>amount</c> is how many were just added, not the resulting stack total.
        /// </summary>
        public static event Action<Character, Item, int>? ItemReceived;

        // ── Internal fire helpers — called by engine code, not by mods ────────

        public static void FireSessionStarted(Character c)              => SessionStarted?.Invoke(c);
        public static void FireLevelUp(Character c, int newLevel)       => LevelUp?.Invoke(c, newLevel);
        public static void FireClassChanged(Character c, string f, string t) => ClassChanged?.Invoke(c, f, t);
        public static void FireRoomEntered(Character c, Room r)         => RoomEntered?.Invoke(c, r);
        public static void FireDayAdvanced(int day)                     => DayAdvanced?.Invoke(day);
        public static void FireMonsterKilled(Character c, Monster m)    => MonsterKilled?.Invoke(c, m);
        public static void FireItemUsed(Character c, Item item)         => ItemUsed?.Invoke(c, item);
        public static void FireItemReceived(Character c, Item item, int amount) => ItemReceived?.Invoke(c, item, amount);
    }
}
