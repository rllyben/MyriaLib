using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Myria.Lib.Core.Systems.Mods
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
    /// <see cref="Myria.Lib.Core.Services.GameService.InitializeGame()"/> when entering a server session.
    /// <see cref="ResolvePath"/> will then skip gameplay mods, ensuring the client uses vanilla data.
    /// Visual mods always apply in both modes.
    /// </para>
    /// </summary>
    public static class ModLoader
    {
        private static readonly JsonSerializerOptions _opts = new() { PropertyNameCaseInsensitive = true };

        private static List<LoadedMod> _mods = new();
        private static List<LoadedMod> _allMods = new();
        private static string _lastModsDirectory = "Mods";

        /// <summary>
        /// Extenders loaded from mod plugin DLLs. Rebuilt on every <see cref="Load"/> call;
        /// separate from the host-registered <see cref="_extenders"/> which are permanent.
        /// </summary>
        private static readonly List<IModLoaderExtender> _pluginExtenders = new();

        // Cache of merged file paths (keyed by defaultPath).
        // Populated lazily on the first ResolvePath call that needs a merge.
        private static readonly Dictionary<string, string> _mergeCache = new();
        private static readonly string _mergeDir =
            Path.Combine(AppContext.BaseDirectory, ".mod_merge_cache");
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
        /// <summary>
        /// Switches multiplayer mode on or off and re-notifies all registered extenders
        /// so they can reload data accordingly — without rescanning the mods directory.
        /// </summary>
        public static void ApplyMultiplayerMode(bool value)
        {
            if (MultiplayerMode == value) return;
            MultiplayerMode = value;
            NotifyAfterLoad(_lastModsDirectory);
        }

        public static void Load(string modsDirectory = "Mods")
        {
            _lastModsDirectory = modsDirectory;
            _mergeCache.Clear();
            if (Directory.Exists(_mergeDir))
                try { Directory.Delete(_mergeDir, recursive: true); } catch { }
            _extenderErrors.Clear();
            _pluginExtenders.Clear();
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

                var allFiles = Directory
                    .EnumerateFiles(dir, "*", SearchOption.AllDirectories)
                    .Select(f => Path.GetRelativePath(dir, f).Replace('\\', '/'))
                    .Where(f => !f.Equals("mod.json", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                // Sidecar files are loaded separately and excluded from override detection.
                var sidecarFiles  = allFiles.Where(IsSidecarFile).ToList();
                var overrideFiles = allFiles.Except(sidecarFiles).ToList();

                // A mod is gameplay-affecting if it has non-visual overrides,
                // non-visual CompleteOverwriteFiles, or an item_effects.json sidecar.
                var isVisual =
                    !HasGameplayManifestContent(manifest)
                    && !sidecarFiles.Any(f => string.Equals(f, "item_effects.json", StringComparison.OrdinalIgnoreCase))
                    && (overrideFiles.Count == 0 || overrideFiles.All(IsVisualPath));

                // Include item_effects.json in the fingerprint so changes to it are detected.
                var filesForFingerprint = overrideFiles
                    .Concat(sidecarFiles.Where(f =>
                        string.Equals(f, "item_effects.json", StringComparison.OrdinalIgnoreCase)));
                var fingerprint = isVisual ? "" : ComputeFingerprint(dir, filesForFingerprint);

                // Load sidecar files.
                var settings     = LoadSidecar<List<ModSettingDefinition>>(dir, "settings.json")      ?? [];
                var settingVals  = LoadSidecar<Dictionary<string, string>>(dir, "setting_values.json") ?? new();
                var visualEffects= LoadSidecar<List<ModVisualEffectDefinition>>(dir, "visual_effects.json") ?? [];
                var itemEffects  = LoadSidecar<List<ModItemUseEffectDefinition>>(dir, "item_effects.json")  ?? [];

                var pluginPath = Path.Combine(dir, "plugin.dll");
                bool hasPlugin = File.Exists(pluginPath)
                              && manifest.Enabled
                              && !(MultiplayerMode && !isVisual); // block gameplay DLLs in multiplayer

                loaded.Add(new LoadedMod
                {
                    Manifest        = manifest,
                    Directory       = dir,
                    IsEnabled       = manifest.Enabled,
                    IsVisualOnly    = isVisual,
                    OverriddenFiles = overrideFiles,
                    Fingerprint     = fingerprint,
                    Settings        = settings,
                    SettingValues   = settingVals,
                    VisualEffects   = visualEffects,
                    ItemEffects     = itemEffects,
                    HasPlugin       = hasPlugin,
                });

                // Load plugin assembly (done after adding to loaded so errors attach to the mod).
                if (hasPlugin)
                    LoadPluginAssembly(loaded[^1], pluginPath);
            }

            _allMods = loaded
                .OrderBy(m => m.Manifest.LoadOrder)
                .ThenBy(m => m.Manifest.Id, StringComparer.OrdinalIgnoreCase)
                .ThenBy(m => m.Directory, StringComparer.OrdinalIgnoreCase)
                .ToList();

            _mods = _allMods
                .Where(m => m.IsEnabled)
                .ToList();

            ValidateMods(_mods);

            ModItemUseEffectRegistry.Load(CreateContext(modsDirectory));
            NotifyAfterLoad(modsDirectory);
        }

        // ── Path resolution ───────────────────────────────────────────────────────

        /// <summary>
        /// Returns the effective file path for <paramref name="defaultPath"/>.
        /// <para>
        /// For JSON data files (non-visual paths), multiple mods are <b>merged</b> together
        /// rather than the first-match-wins approach used for binary/visual assets:
        /// <list type="bullet">
        ///   <item>Objects whose ID matches an existing entry patch only the fields they define.</item>
        ///   <item>Objects with <c>"overwrite": true</c> replace the entire matched entry.</item>
        ///   <item>A property key ending with <c>+</c> (e.g. <c>"Npcs+"</c>) appends to or merges
        ///         with the existing value instead of replacing it: arrays are concatenated, objects
        ///         have their keys merged recursively.</item>
        ///   <item>A mod that lists the file in <see cref="ModManifest.CompleteOverwriteFiles"/>
        ///         discards all previous content before applying its own (later mods can still layer on top).</item>
        /// </list>
        /// </para>
        /// For non-JSON and visual files the highest-priority mod wins (original behaviour).
        /// </summary>
        public static string ResolvePath(string defaultPath)
        {
            // Collect the mods that provide this file, respecting MultiplayerMode.
            var modsWithFile = _mods
                .Where(m => !(MultiplayerMode && !m.IsVisualOnly))
                .Where(m => File.Exists(Path.Combine(m.Directory, defaultPath)))
                .ToList();

            if (modsWithFile.Count == 0)
                return defaultPath;

            // All JSON files — including locale files — are merged so multiple mods can
            // each contribute keys/objects without clobbering one another.
            // Non-JSON files (XAML, images, icons) use last-mod-wins.
            bool isJson = defaultPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase);

            if (!isJson)
            {
                // Last (highest-priority) mod wins for binary / XAML / image assets.
                return Path.Combine(modsWithFile[^1].Directory, defaultPath);
            }

            // All JSON files: merge (key-value merge for locale objects, array merge for game data).
            // Use a cached temp file if we already built it.
            if (_mergeCache.TryGetValue(defaultPath, out var cached) && File.Exists(cached))
                return cached;

            var mergedPath = BuildMergedFile(defaultPath, modsWithFile);
            _mergeCache[defaultPath] = mergedPath;
            return mergedPath;
        }

        // ── Merge engine ─────────────────────────────────────────────────────────

        /// <summary>
        /// Merges the base file with each mod's version and writes the result to a temp file.
        /// Mods are applied in load order (ascending); each can either patch or completely replace.
        /// </summary>
        private static string BuildMergedFile(string defaultPath, List<LoadedMod> mods)
        {
            // Start from the base file (may not exist if it's a pure-addition mod target).
            string currentJson = File.Exists(defaultPath)
                ? File.ReadAllText(defaultPath)
                : "[]";

            foreach (var mod in mods)
            {
                var modFile  = Path.Combine(mod.Directory, defaultPath);
                var modJson  = File.ReadAllText(modFile);
                var isComplete = mod.Manifest.CompleteOverwriteFiles
                    .Any(f => string.Equals(
                        f.Replace('\\', '/').Trim('/'),
                        defaultPath.Replace('\\', '/').Trim('/'),
                        StringComparison.OrdinalIgnoreCase));

                currentJson = isComplete ? modJson : MergeJson(currentJson, modJson);
            }

            Directory.CreateDirectory(_mergeDir);
            var hash    = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(defaultPath)))[..12];
            var outPath = Path.Combine(_mergeDir, hash + "_" + Path.GetFileName(defaultPath));
            File.WriteAllText(outPath, currentJson);
            return outPath;
        }

        /// <summary>
        /// Merges <paramref name="patchJson"/> into <paramref name="baseJson"/>.
        /// Supports both JSON arrays (matched by object ID) and plain JSON objects (merged by key).
        /// </summary>
        private static string MergeJson(string baseJson, string patchJson)
        {
            JsonNode? baseNode  = JsonNode.Parse(baseJson);
            JsonNode? patchNode = JsonNode.Parse(patchJson);

            if (baseNode is JsonArray baseArr && patchNode is JsonArray patchArr)
                return MergeJsonArrays(baseArr, patchArr).ToJsonString(new JsonSerializerOptions { WriteIndented = true });

            if (baseNode is JsonObject baseObj && patchNode is JsonObject patchObj)
                return MergeJsonObjects(baseObj, patchObj).ToJsonString(new JsonSerializerOptions { WriteIndented = true });

            // Incompatible shapes — patch wins.
            return patchJson;
        }

        private static JsonArray MergeJsonArrays(JsonArray baseArr, JsonArray patchArr)
        {
            // Build an index into the base array keyed by object ID.
            var lookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < baseArr.Count; i++)
            {
                if (baseArr[i] is JsonObject obj)
                {
                    var id = GetObjectId(obj);
                    if (id != null) lookup[id] = i;
                }
            }

            foreach (var patchNode in patchArr)
            {
                if (patchNode is not JsonObject patchObj) continue;

                var id = GetObjectId(patchObj);
                bool completeOverwrite = patchObj["overwrite"]?.GetValue<bool>() ?? false;

                // Clone and strip the internal "overwrite" flag before writing.
                var clean = (JsonObject)JsonNode.Parse(patchObj.ToJsonString())!;
                clean.Remove("overwrite");

                if (id != null && lookup.TryGetValue(id, out int idx))
                {
                    if (completeOverwrite)
                    {
                        baseArr[idx] = clean;
                    }
                    else
                    {
                        // Merge the patch's fields into the existing entry.
                        // Supports "property+" append syntax via MergeJsonObjects.
                        var target = (JsonObject)baseArr[idx]!;
                        MergeJsonObjects(target, clean);
                    }
                }
                else
                {
                    // New entry not present in base — append it.
                    baseArr.Add(clean);
                }
            }

            return baseArr;
        }

        /// <summary>
        /// Merges <paramref name="patchObj"/> into <paramref name="baseObj"/> property by property.
        /// <para>
        /// Standard properties replace the base value. A property whose key ends with <c>+</c>
        /// uses additive merge instead of replacement:
        /// <list type="bullet">
        ///   <item>JSON array   → all items in the patch are appended to the base array.</item>
        ///   <item>JSON object  → patch keys are recursively merged into the base object.</item>
        ///   <item>Scalar / type mismatch → falls back to replace (same as without <c>+</c>).</item>
        /// </list>
        /// The <c>+</c> suffix is stripped from the key before writing to the output.
        /// </para>
        /// </summary>
        private static JsonObject MergeJsonObjects(JsonObject baseObj, JsonObject patchObj)
        {
            foreach (var kv in patchObj)
            {
                if (kv.Key.EndsWith('+'))
                {
                    var baseKey = kv.Key[..^1];
                    AppendOrMergeProperty(baseObj, baseKey, kv.Value);
                }
                else
                {
                    baseObj[kv.Key] = kv.Value?.DeepClone();
                }
            }
            return baseObj;
        }

        /// <summary>
        /// Appends <paramref name="patchValue"/> to or merges it with the existing property at
        /// <paramref name="key"/> in <paramref name="target"/>.
        /// Called by <see cref="MergeJsonObjects"/> for properties with a <c>+</c> suffix.
        /// </summary>
        private static void AppendOrMergeProperty(JsonObject target, string key, JsonNode? patchValue)
        {
            if (patchValue is null) return;

            // Find the existing property, case-insensitively.
            var existingKey = target
                .Select(kv => kv.Key)
                .FirstOrDefault(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase))
                ?? key;

            var existing = target[existingKey];

            if (existing is JsonArray existingArr && patchValue is JsonArray patchArr)
            {
                // Append all patch items to the existing array.
                foreach (var item in patchArr)
                    existingArr.Add(item?.DeepClone());
            }
            else if (existing is JsonObject existingObj && patchValue is JsonObject patchObj)
            {
                // Recursively merge the objects.
                MergeJsonObjects(existingObj, patchObj);
            }
            else if (existing is null)
            {
                // Property doesn't exist yet — create it.
                target[key] = patchValue.DeepClone();
            }
            else
            {
                // Types don't match or it's a scalar — fall back to replace.
                target[existingKey] = patchValue.DeepClone();
            }
        }

        /// <summary>Tries common ID field names to identify a JSON object in the array.</summary>
        private static string? GetObjectId(JsonObject obj)
        {
            foreach (var key in new[] { "Id", "id", "Name", "name" })
            {
                if (obj.TryGetPropertyValue(key, out var val) && val is JsonValue jv)
                    try { return jv.GetValue<string>(); } catch { }
            }
            return null;
        }

        // ── Server handshake ─────────────────────────────────────────────────────

        /// <summary>
        /// Returns all language definitions provided by currently active mods, in load order.
        /// Used by the language selector to display mod-provided languages alongside built-in ones.
        /// </summary>
        public static IReadOnlyList<(LoadedMod Mod, ModLanguageDefinition Def)> GetModLanguages()
            => _mods
                .SelectMany(m => m.Manifest.LanguageDefinitions
                    .Where(d => !string.IsNullOrWhiteSpace(d.Id) && !string.IsNullOrWhiteSpace(d.LocaleFile))
                    .Select(d => (m, d)))
                .ToList();

        /// <summary>
        /// Returns a <see cref="ModSnapshot"/> capturing the currently active gameplay mods.
        /// This is stored in every character save file so loading can detect mod drift.
        /// </summary>
        public static ModSnapshot GetCurrentSnapshot() => new ModSnapshot
        {
            Mods = _mods
                .Where(m => !m.IsVisualOnly)
                .OrderBy(m => m.Manifest.Id, StringComparer.OrdinalIgnoreCase)
                .Select(m => new ModSnapshotEntry
                {
                    Id          = m.Manifest.Id,
                    Version     = m.Manifest.Version,
                    Fingerprint = m.Fingerprint,
                })
                .ToList()
        };

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
            => manifest.CompleteOverwriteFiles.Any(f =>
                    !IsVisualPath(f.ToLowerInvariant().Replace('\\', '/')));

        /// <summary>Files that are loaded as sidecar data rather than as game-data overrides.</summary>
        private static bool IsSidecarFile(string relativePath) =>
            string.Equals(relativePath, "settings.json",       StringComparison.OrdinalIgnoreCase)
         || string.Equals(relativePath, "setting_values.json", StringComparison.OrdinalIgnoreCase)
         || string.Equals(relativePath, "visual_effects.json", StringComparison.OrdinalIgnoreCase)
         || string.Equals(relativePath, "item_effects.json",   StringComparison.OrdinalIgnoreCase);

        private static T? LoadSidecar<T>(string modDirectory, string filename) where T : class
        {
            var path = Path.Combine(modDirectory, filename);
            if (!File.Exists(path)) return null;
            try { return JsonSerializer.Deserialize<T>(File.ReadAllText(path), _opts); }
            catch { return null; }
        }

        // ── Validation ────────────────────────────────────────────────────────

        /// <summary>
        /// Checks each enabled mod for game-version compatibility and dependency satisfaction.
        /// Mods with blocking problems are removed from <paramref name="activeMods"/> and have
        /// their <see cref="LoadedMod.Warnings"/> populated with the reason(s).
        /// Optional-dependency failures produce warnings only and do not block the mod.
        /// </summary>
        private static void ValidateMods(List<LoadedMod> activeMods)
        {
            var byId = activeMods.ToDictionary(
                m => m.Manifest.Id,
                m => m,
                StringComparer.OrdinalIgnoreCase);

            var blocked = new List<LoadedMod>();

            foreach (var mod in activeMods)
            {
                bool hasBlocker = false;

                // ── Game version ──────────────────────────────────────────────
                if (!string.IsNullOrWhiteSpace(mod.Manifest.MinGameVersion)
                    && Version.TryParse(mod.Manifest.MinGameVersion, out var minVer)
                    && Myria.Lib.Core.Systems.GameVersion.Current < minVer)
                {
                    mod.Warnings.Add(
                        $"Requires game version ≥ {mod.Manifest.MinGameVersion} " +
                        $"(running {Myria.Lib.Core.Systems.GameVersion.Current}).");
                    hasBlocker = true;
                }

                if (!string.IsNullOrWhiteSpace(mod.Manifest.MaxGameVersion)
                    && Version.TryParse(mod.Manifest.MaxGameVersion, out var maxVer)
                    && Myria.Lib.Core.Systems.GameVersion.Current > maxVer)
                {
                    mod.Warnings.Add(
                        $"Incompatible with game version > {mod.Manifest.MaxGameVersion} " +
                        $"(running {Myria.Lib.Core.Systems.GameVersion.Current}).");
                    hasBlocker = true;
                }

                // ── Dependencies ──────────────────────────────────────────────
                foreach (var dep in mod.Manifest.Dependencies)
                {
                    if (string.IsNullOrWhiteSpace(dep.Id)) continue;

                    if (!byId.TryGetValue(dep.Id, out var depMod))
                    {
                        var msg = $"Required mod \"{dep.Id}\" is not active.";
                        mod.Warnings.Add(dep.Optional ? $"[Optional] {msg}" : msg);
                        if (!dep.Optional) hasBlocker = true;
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(dep.MinVersion)
                        && Version.TryParse(dep.MinVersion, out var depMin)
                        && Version.TryParse(depMod.Manifest.Version, out var depVer)
                        && depVer < depMin)
                    {
                        var msg = $"Requires \"{dep.Id}\" version ≥ {dep.MinVersion} " +
                                  $"(active version: {depMod.Manifest.Version}).";
                        mod.Warnings.Add(dep.Optional ? $"[Optional] {msg}" : msg);
                        if (!dep.Optional) hasBlocker = true;
                    }
                }

                if (hasBlocker) blocked.Add(mod);
            }

            foreach (var mod in blocked)
                activeMods.Remove(mod);
        }

        private static void LoadPluginAssembly(LoadedMod mod, string pluginPath)
        {
            try
            {
                var assembly = Assembly.LoadFrom(pluginPath);
                foreach (var type in assembly.GetExportedTypes())
                {
                    if (!typeof(IModLoaderExtender).IsAssignableFrom(type)
                        || type.IsAbstract || type.IsInterface)
                        continue;
                    try
                    {
                        var extender = (IModLoaderExtender)Activator.CreateInstance(type)!;
                        _pluginExtenders.Add(extender);
                    }
                    catch (Exception ex)
                    {
                        _extenderErrors.Add(new ModExtenderError
                        {
                            ExtenderName = type.FullName ?? type.Name,
                            Phase        = "plugin instantiate",
                            Exception    = ex,
                            Mod          = mod
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _extenderErrors.Add(new ModExtenderError
                {
                    ExtenderName = pluginPath,
                    Phase        = "plugin load",
                    Exception    = ex,
                    Mod          = mod
                });
            }
        }

        private static void NotifyBeforeReload(string modsDirectory)
        {
            var context = CreateContext(modsDirectory);
            foreach (var extender in _extenders.Concat(_pluginExtenders).ToList())
                TryNotify(extender, "BeforeModsReload", () => extender.BeforeModsReload(context));
        }

        private static void NotifyAfterLoad(string modsDirectory)
        {
            var context = CreateContext(modsDirectory);
            foreach (var extender in _extenders.Concat(_pluginExtenders).ToList())
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
