using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace HoppinHauler.ExtendedRandomMoons
{
    internal static class Util
    {
        private static readonly Regex LeadingDigits = new Regex("^[0-9]+", RegexOptions.Compiled);

        public static void ParseCsvToNormalizedSet(string csv, HashSet<string> set)
        {
            set.Clear();
            if (string.IsNullOrWhiteSpace(csv)) return;

            string[] parts = csv.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                string token = parts[i] == null ? null : parts[i].Trim();
                if (string.IsNullOrWhiteSpace(token)) continue;

                set.Add(NormalizeMoonName(token));
            }
        }

        public static string NormalizeMoonName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;

            string s = LeadingDigits.Replace(name.Trim(), string.Empty);
            s = s.Trim();

            if (s.StartsWith("-", StringComparison.InvariantCulture)) s = s.Substring(1).Trim();
            return s;
        }

        public static string NormalizeWeatherToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;

            string s = token.Trim().ToLowerInvariant();
            s = s.Replace(" ", "");
            s = s.Replace("_", "");
            s = s.Replace("-", "");

            if (s == "dustcloud" || s == "dustclouds") return "dustclouds";
            if (s == "none") return "mild";
            return s;
        }

        public static void ExpandWeatherAliases(HashSet<string> set)
        {
            var list = new List<string>(set);
            set.Clear();

            for (int i = 0; i < list.Count; i++)
            {
                string k = NormalizeWeatherToken(list[i]);
                if (!string.IsNullOrEmpty(k)) set.Add(k);
            }

            if (set.Contains("none")) { set.Remove("none"); set.Add("mild"); }
        }

        public static object[] TryGetLevelsArray(object startOfRoundInstance)
        {
            object levelsObj = TryGetMemberValue(startOfRoundInstance, "levels");
            Array arr = levelsObj as Array;
            if (arr == null) return null;

            object[] managed = new object[arr.Length];
            for (int i = 0; i < arr.Length; i++) managed[i] = arr.GetValue(i);
            return managed;
        }

        public static object TryGetCurrentLevel(object startOfRoundInstance)
        {
            return TryGetMemberValue(startOfRoundInstance, "currentLevel");
        }

        public static string TryGetPlanetName(object selectableLevel)
        {
            object v = TryGetMemberValue(selectableLevel, "PlanetName")
                       ?? TryGetMemberValue(selectableLevel, "planetName")
                       ?? TryGetMemberValue(selectableLevel, "name");
            return v as string;
        }

        public static int TryGetLevelId(object selectableLevel, int fallbackIndex)
        {
            object v = TryGetMemberValue(selectableLevel, "levelID") ?? TryGetMemberValue(selectableLevel, "LevelID");
            if (v is int i) return i;
            return fallbackIndex;
        }

        public static bool IsCompanyMoonHeuristic(object selectableLevel)
        {
            object planetHasTime = TryGetMemberValue(selectableLevel, "planetHasTime");
            object spawnEnemiesAndScrap = TryGetMemberValue(selectableLevel, "spawnEnemiesAndScrap");

            bool? a = planetHasTime as bool?;
            bool? b = spawnEnemiesAndScrap as bool?;

            if (a.HasValue && b.HasValue)
                return (!a.Value && !b.Value);

            return false;
        }

        public static object TryGetMemberValue(object instance, string name)
        {
            if (instance == null || string.IsNullOrEmpty(name)) return null;

            Type t = instance.GetType();
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

            var f = t.GetField(name, flags);
            if (f != null) return f.GetValue(instance);

            var p = t.GetProperty(name, flags);
            if (p != null && p.GetIndexParameters().Length == 0) return p.GetValue(instance, null);

            return null;
        }

        public static bool TrySetMemberValue(object instance, string name, object value)
        {
            if (instance == null || string.IsNullOrEmpty(name)) return false;

            Type t = instance.GetType();
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

            var f = t.GetField(name, flags);
            if (f != null)
            {
                try { f.SetValue(instance, value); return true; }
                catch { return false; }
            }

            var p = t.GetProperty(name, flags);
            if (p != null && p.GetIndexParameters().Length == 0 && p.CanWrite)
            {
                try { p.SetValue(instance, value, null); return true; }
                catch { return false; }
            }

            return false;
        }

        public static string TryGetStringMember(object instance, string name)
        {
            object v = TryGetMemberValue(instance, name);
            return v as string;
        }

        public static int TryGetIntMember(object instance, string name, int fallback)
        {
            object v = TryGetMemberValue(instance, name);
            return v is int i ? i : fallback;
        }

        public static Array AppendToArray(Array array, object item)
        {
            if (array == null)
            {
                Array single = Array.CreateInstance(item.GetType(), 1);
                single.SetValue(item, 0);
                return single;
            }

            Type elementType = array.GetType().GetElementType();
            Array result = Array.CreateInstance(elementType, array.Length + 1);
            Array.Copy(array, result, array.Length);
            result.SetValue(item, array.Length);
            return result;
        }

        public static string SanitizeId(string s)
        {
            if (string.IsNullOrEmpty(s)) return "unknown";
            s = s.Trim().ToLowerInvariant();
            s = Regex.Replace(s, "[^a-z0-9]+", "_");
            return s.Trim('_');
        }

        public static int TryGetGroupCredits(object terminalInstance, int fallback)
        {
            object v = TryGetMemberValue(terminalInstance, "groupCredits");
            if (v is int i) return i;
            return fallback;
        }
    }
}
