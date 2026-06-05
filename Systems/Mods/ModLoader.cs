using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MyriaLib.Systems.Mods
{
    /// <summary>
    /// Scans a <c>Mods/</c> directory, loads <c>mod.json</c> manifests, and resolves file paths
    /// so every data loader transparently picks up mod overrides.
    /// <para>
    /// A mod is <b>visual-only</b> if all its files live under <c>Data/locales/</c>,
    /// <c>Data/Icons/</c>, <c>Data/images/</c>, <c>Data/Maps/</c>, or <c>Assets/</c>.
    /// All other files (anything under <c>Data/common/</c>) are considered gameplay-affecting.
    /// </para>
    /// <para>
    /// Set <see cref="MultiplayerMode"/> = <c>true</c> before calling
    /// <see cref="MyriaLib.Services.GameService.InitializeGame()"/> when entering a server session.
    /// <see cref="ResolvePath"/> will then skip gameplay mods, ensuring the client uses vanilla data.
    /// Visual mods always apply in both modes.
    /// </para>
    /// </summary>
    public static class ModLoader
    {
        private static readonly JsonSerializerOptions _opts = new() { PropertyNameCaseInsensitive = true };

        private static List<LoadedMod> _mods = new();

        private static readonly string[] _visualPrefixes =
        {
            "data/locales/", "data/icons/", "data/images/", "data/maps/", "assets/"
        };

        /// <summary>All loaded mods in ascending <see cref="ModManifest.LoadOrder"/> order.</summary>
        public static IReadOnlyList<LoadedMod> ActiveMods => _mods;

        /// <summary>Subset of <see cref="ActiveMods"/> that override gameplay data.</summary>
        public static IReadOnlyList<LoadedMod> GameplayMods
            => _mods.Where(m => !m.IsVisualOnly).ToList();

        /// <summary>
        /// When <c>true</c>, <see cref="ResolvePath"/> ignores gameplay-affecting mod overrides
        /// so the client loads unmodified game data for multiplayer sessions.
        /// Visual mods are always applied regardless of this flag.
        /// </summary>
        public static bool MultiplayerMode { get; set; }

        // ── Loading ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Scans <paramref name="modsDirectory"/> for subfolders that contain a <c>mod.json</c>,
        /// loads each manifest, and registers mods sorted by <see cref="ModManifest.LoadOrder"/>.
        /// Safe to call multiple times; previous mods are discarded.
        /// Silently returns if the directory does not exist.
        /// </summary>
        public static void Load(string modsDirectory = "Mods")
        {
            _mods.Clear();
            if (!Directory.Exists(modsDirectory)) return;

            var loaded = new List<LoadedMod>();

            foreach (var dir in Directory.EnumerateDirectories(modsDirectory))
            {
                var manifestPath = Path.Combine(dir, "mod.json");
                if (!File.Exists(manifestPath)) continue;

                ModManifest manifest;
                try
                {
                    manifest = JsonSerializer.Deserialize<ModManifest>(
                                   File.ReadAllText(manifestPath), _opts)
                               ?? new ModManifest { Id = Path.GetFileName(dir) };
                }
                catch { continue; }

                if (string.IsNullOrWhiteSpace(manifest.Id))
                    manifest.Id = Path.GetFileName(dir);

                var files = Directory
                    .EnumerateFiles(dir, "*", SearchOption.AllDirectories)
                    .Select(f => Path.GetRelativePath(dir, f).Replace('\\', '/'))
                    .Where(f => !f.Equals("mod.json", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var isVisual    = files.Count == 0 || files.All(IsVisualPath);
                var fingerprint = isVisual ? "" : ComputeFingerprint(dir, files);

                loaded.Add(new LoadedMod
                {
                    Manifest        = manifest,
                    Directory       = dir,
                    IsVisualOnly    = isVisual,
                    OverriddenFiles = files,
                    Fingerprint     = fingerprint,
                });
            }

            _mods = loaded.OrderBy(m => m.Manifest.LoadOrder).ToList();
        }

        // ── Path resolution ───────────────────────────────────────────────────────

        /// <summary>
        /// Returns the effective file path for <paramref name="defaultPath"/>.
        /// Mods are checked in reverse load-order so the highest-priority mod wins.
        /// In <see cref="MultiplayerMode"/>, gameplay mods are skipped; only visual overrides apply.
        /// Returns <paramref name="defaultPath"/> when no mod provides a matching file.
        /// </summary>
        public static string ResolvePath(string defaultPath)
        {
            foreach (var mod in Enumerable.Reverse(_mods))
            {
                if (MultiplayerMode && !mod.IsVisualOnly) continue;
                var candidate = Path.Combine(mod.Directory, defaultPath);
                if (File.Exists(candidate)) return candidate;
            }
            return defaultPath;
        }

        // ── Server handshake ─────────────────────────────────────────────────────

        /// <summary>Builds a <see cref="ModInfo"/> snapshot suitable for sending to the server on connection.</summary>
        public static ModInfo GetModInfo() => new()
        {
            ActiveMods = _mods.Select(m => new ModEntry
            {
                Id           = m.Manifest.Id,
                Name         = m.Manifest.Name,
                Version      = m.Manifest.Version,
                IsVisualOnly = m.IsVisualOnly,
                Fingerprint  = m.Fingerprint,
            }).ToList()
        };

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static bool IsVisualPath(string relativePath)
        {
            var n = relativePath.ToLowerInvariant().Replace('\\', '/');
            return _visualPrefixes.Any(p => n.StartsWith(p));
        }

        private static string ComputeFingerprint(string modDir, IEnumerable<string> files)
        {
            var sb = new StringBuilder();
            foreach (var rel in files.Where(f => !IsVisualPath(f)).OrderBy(f => f, StringComparer.Ordinal))
            {
                var full = Path.Combine(modDir, rel);
                if (!File.Exists(full)) continue;
                sb.Append(rel).Append(':')
                  .Append(Convert.ToBase64String(SHA256.HashData(File.ReadAllBytes(full))))
                  .Append(';');
            }
            if (sb.Length == 0) return "";
            return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString())));
        }
    }
}
