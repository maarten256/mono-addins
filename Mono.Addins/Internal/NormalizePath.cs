//
// NormalizePath.cs
//
// Mimics Mono.Util.NormalizePath from early Mono versions.
// Used by Mono.Addins to canonicalize paths.
// Originally part of Mono internal utils.
//

using System;
using System.Collections.Generic;
using System.Text;

namespace Mono.Addins.Compat
{
    public static class NormalizePath
    {
        public static string Normalize(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            path = path.Replace('\\', '/');

            var parts = new Stack<string>();
            foreach (string segment in path.Split('/'))
            {
                if (segment == "..")
                {
                    if (parts.Count > 0)
                        parts.Pop();
                }
                else if (segment != "." && segment.Length > 0)
                {
                    parts.Push(segment);
                }
            }

            // Stack reverses the order, so build the path manually
            var result = new StringBuilder();
            string[] items = parts.ToArray();
            for (int i = items.Length - 1; i >= 0; i--)
            {
                result.Append('/');
                result.Append(items[i]);
            }

            // Return root if empty
            return result.Length == 0 ? "/" : result.ToString();
        }
    }
}
