using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyriaLib.Entities.NPCs
{
    public sealed class NpcActionResult
    {
        public bool Success { get; init; }
        public string MessageKey { get; init; } = "";   // for Localization.T(...)
        public object[] MessageArgs { get; init; } = System.Array.Empty<object>();

        public static NpcActionResult Ok(string key, params object[] args)
            => new() { Success = true, MessageKey = key, MessageArgs = args };

        public static NpcActionResult Fail(string key, params object[] args)
            => new() { Success = false, MessageKey = key, MessageArgs = args };
    }

}
