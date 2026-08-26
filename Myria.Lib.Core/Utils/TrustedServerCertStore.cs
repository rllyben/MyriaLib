using System.Text.Json;

namespace Myria.Lib.Core.Utils
{
    /// <summary>
    /// Trust-on-first-use (TOFU) store for self-signed server certificates, keyed by
    /// "host:port". Lets a client connect to ANY operator-run server address - not just one
    /// hardcoded official server - while still detecting a certificate that changes after the
    /// first successful connection (a cert rotation the player should be warned about, or a
    /// possible MITM), the same trust model SSH uses for host keys.
    /// A real CA-issued certificate never needs this - it's only consulted when the default
    /// TLS chain validation already failed (self-signed/untrusted-root).
    /// </summary>
    public static class TrustedServerCertStore
    {
        private static readonly string PathFile = Path.Combine("Data", "Misc", "trusted_certs.json");
        private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };
        private static Dictionary<string, string>? _cache;

        /// <summary>
        /// Returns true if <paramref name="thumbprint"/> should be trusted for <paramref name="host"/>:
        /// either it matches what was already trusted for this host, or nothing was trusted for
        /// this host yet (first connection - trust and remember it). Returns false if this host
        /// already has a *different* trusted thumbprint on file.
        /// </summary>
        public static bool TrustOnFirstUse(string host, string thumbprint)
        {
            var store = Load();
            if (store.TryGetValue(host, out var trusted))
                return string.Equals(trusted, thumbprint, StringComparison.OrdinalIgnoreCase);

            store[host] = thumbprint;
            Save(store);
            return true;
        }

        /// <summary>Clears a previously-trusted certificate for a host, e.g. after a legitimate
        /// cert rotation the player has confirmed out-of-band - the next connection will
        /// trust-on-first-use again.</summary>
        public static void Forget(string host)
        {
            var store = Load();
            if (store.Remove(host))
                Save(store);
        }

        private static Dictionary<string, string> Load()
        {
            if (_cache is not null) return _cache;

            try
            {
                if (File.Exists(PathFile))
                {
                    var json = File.ReadAllText(PathFile);
                    _cache = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
                    return _cache;
                }
            }
            catch
            {
                // Corrupt/unreadable store - fall through to a fresh one rather than blocking
                // every connection on a local-file problem.
            }

            _cache = new Dictionary<string, string>();
            return _cache;
        }

        private static void Save(Dictionary<string, string> store)
        {
            _cache = store;
            Directory.CreateDirectory(Path.GetDirectoryName(PathFile)!);
            File.WriteAllText(PathFile, JsonSerializer.Serialize(store, JsonOpts));
        }
    }
}
