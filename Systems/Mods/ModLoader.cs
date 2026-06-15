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
        private static List<LoadedMod> _allMods = new();
        private static readonly List<IModLoaderExtender> _extenders = new();
        private static readonly List<ModExtenderError> _extenderErrors = new();

        private static readonly string[] _visualPrefixes =
        {
            "data/locales/", "data/icons/", "data/images/", "data/maps/", "assets/"
        };

        /// <summary>All loaded mods in ascending <see cref="ModManifest.LoadOrder"/> order.</summary>
        public static IReadOnlyList<LoadedMod> ActiveMods => _mods;

        /// <summary>All discovered mods, including disabled mods, in configured load order.</summary>
        public static IReadOnlyList<LoadedMod> AllMods => _allMods;

        /// <summary>Subset of <see cref="ActiveMods"/> whose files are visual-only.</summary>
        public static IReadOnlyList<LoadedMod> VisualMods
            => _mods.Where(m => m.IsVisualOnly).ToList();

        /// <summary>Subset of <see cref="ActiveMods"/> that override gameplay data.</summary>
        public static IReadOnlyList<LoadedMod> GameplayMods
            => _mods.Where(m => !m.IsVisualOnly).ToList();

        /// <summary>Project-specific extenders registered by UI hosts or other frontends.</summary>
        public static IReadOnlyList<IModLoaderExtender> Extenders => _extenders;

        /// <summary>Errors thrown by extenders during the most recent <see cref="Load"/> call.</summary>
        public static IReadOnlyList<ModExtenderError> ExtenderErrors => _extenderErrors;

        /// <summary>
        /// When <c>true</c>, <see cref="ResolvePath"/> ignores gameplay-affecting mod overrides
        /// so the client loads unmodified game data for multiplayer sessions.
        /// Visual mods are always applied regardless of this flag.
        /// </summary>
        public static bool MultiplayerMode { get; set; }

        /// <summary>Registers a project-specific extender that will be notified whenever mods are reloaded.</summary>
        public static void RegisterExtender(IModLoaderExtender extender)
        {
            if (_extenders.Contains(extender)) return;
            _extenders.Add(extender);
        }

        /// <summary>Removes a previously registered extender.</summary>
        public static bool UnregisterExtender(IModLoaderExtender extender)
            => _extenders.Remove(extender);

        /// <summary>Removes all registered extenders.</summary>
        public static void ClearExtenders() => _extenders.Clear();

        /// <summary>Unloads a single active mod after a host-specific loader failed to apply it.</summary>
        public static void UnloadModAfterError(
            LoadedMod mod,
            string phase,
            Exception exception,
            string extenderName)
        {
            if (_mods.Remove(mod))
            {
                _extenderErrors.Add(new ModExtenderError
                {
                    ExtenderName = extenderName,
                    Phase = phase,
                    Exception = exception,
                    Mod = mod,
                    ModUnloaded = true
                });
            }
        }

        // ── Loading ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Scans <paramref name="modsDirectory"/> for subfolders that contain a <c>mod.json</c>,
        /// loads each manifest, and registers mods sorted by <see cref="ModManifest.LoadOrder"/>.
        /// Safe to call multiple times; previous mods are discarded.
        /// Silently returns if the directory does not exist.
        /// </summary>
        public static void Load(string modsDirectory = "Mods")
        {
            _extenderErrors.Clear();
            NotifyBeforeReload(modsDirectory);

            _mods.Clear();
            _allMods.Clear();
            if (!Directory.Exists(modsDirectory))
            {
                ModItemUseEffectRegistry.Load(CreateContext(modsDirectory));
                NotifyAfterLoad(modsDirectory);
                return;
            }

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

                var isVisual    = !HasGameplayManifestContent(manifest)
                                  && (files.Count == 0 || files.All(IsVisualPath));
                var fingerprint = isVisual ? "" : ComputeFingerprint(dir, files);

                loaded.Add(new LoadedMod
                {
                    Manifest        = manifest,
                    Directory       = dir,
                    IsEnabled       = manifest.Enabled,
                    IsVisualOnly    = isVisual,
                    OverriddenFiles = files,
                    Fingerprint     = fingerprint,
                });
            }

            _allMods = loaded
                .OrderBy(m => m.Manifest.LoadOrder)
                .ThenBy(m => m.Manifest.Id, StringComparer.OrdinalIgnoreCase)
                .ThenBy(m => m.Directory, StringComparer.OrdinalIgnoreCase)
                .ToList();

            _mods = _allMods
                .Where(m => m.IsEnabled)
                .ToList();

            ModItemUseEffectRegistry.Load(CreateContext(modsDirectory));
            NotifyAfterLoad(modsDirectory);
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

        private static bool HasGameplayManifestContent(ModManifest manifest)
            => manifest.ItemAdditions.Count > 0
            || manifest.ItemUseEffects.Count > 0
            || manifest.NpcItemAdditions.Count > 0;

        private static void NotifyBeforeReload(string modsDirectory)
        {
            var context = CreateContext(modsDirectory);
            foreach (var extender in _extenders.ToList())
                TryNotify(extender, "BeforeModsReload", () => extender.BeforeModsReload(context));
        }

        private static void NotifyAfterLoad(string modsDirectory)
        {
            var context = CreateContext(modsDirectory);
            foreach (var extender in _extenders.ToList())
                TryNotify(extender, "AfterModsLoaded", () => extender.AfterModsLoaded(context));
        }

        private static ModLoadContext CreateContext(string modsDirectory)
            => new(modsDirectory, _mods.ToList(), MultiplayerMode);

        private static void TryNotify(IModLoaderExtender extender, string phase, Action notify)
        {
            try
            {
                notify();
            }
            catch (Exception ex)
            {
                _extenderErrors.Add(new ModExtenderError
                {
                    ExtenderName = extender.GetType().FullName ?? extender.GetType().Name,
                    Phase = phase,
                    Exception = ex
                });
            }
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
