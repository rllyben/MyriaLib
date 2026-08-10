namespace Myria.Lib.Core.Systems.Mods
{
    public class ModSnapshot
    {
        /// <summary>Gameplay mods active at save time, sorted by ID.</summary>
        public List<ModSnapshotEntry> Mods { get; set; } = new();

        public bool Matches(ModSnapshot current)
        {
            var changes = GetChanges(current);
            return changes.Added.Count == 0
                && changes.Removed.Count == 0
                && changes.Changed.Count == 0;
        }

        public ModChangeSet GetChanges(ModSnapshot current)
        {
            var saved    = Mods.ToDictionary(m => m.Id, m => m);
            var now      = current.Mods.ToDictionary(m => m.Id, m => m);

            var added   = now.Keys.Except(saved.Keys).ToList();
            var removed = saved.Keys.Except(now.Keys).ToList();
            var changed = saved.Keys.Intersect(now.Keys)
                .Where(id => saved[id].Version     != now[id].Version
                          || saved[id].Fingerprint != now[id].Fingerprint)
                .Select(id => (id, saved[id].Version, now[id].Version))
                .ToList();

            return new ModChangeSet(added, removed, changed);
        }
    }

    public class ModSnapshotEntry
    {
        public string Id          { get; set; } = "";
        public string Version     { get; set; } = "";
        public string Fingerprint { get; set; } = "";
    }

    public record ModChangeSet(
        List<string>                          Added,
        List<string>                          Removed,
        List<(string Id, string Was, string Now)> Changed);
}
