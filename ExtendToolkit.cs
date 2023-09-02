using MinecraftLaunch.Modules.Models.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace System {
    public static class ExtendToolkit {
        public static string ToUri(this string raw) {
            if (raw.EndsWith('/')) {
                return Regex.Replace(raw, ".$", "");
            } else {
                return raw;
            }
        }

        public static async ValueTask<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> raw) {
            List<T> list = new();
            await foreach (var i in raw) {
                list.Add(i);
            }

            return list;
        }

        public static List<T> ToList<T>(this IAsyncEnumerable<T> raw) => ToListAsync(raw).Result;
    }
}
