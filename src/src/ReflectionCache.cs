using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Reflection;

namespace HoppinHauler.ExtendedRandomMoons
{
    internal static class ReflectionCache
    {
        internal static Type TerminalType;
        internal static Type TerminalNodeType;
        internal static Type TerminalKeywordType;
        internal static Type CompatibleNounType;

        internal static Type StartOfRoundType;
        internal static Type SelectableLevelType;

        internal static void Warmup(ManualLogSource log)
        {
            TerminalType = AccessTools.TypeByName("Terminal");
            TerminalNodeType = AccessTools.TypeByName("TerminalNode");
            TerminalKeywordType = AccessTools.TypeByName("TerminalKeyword");
            CompatibleNounType = AccessTools.TypeByName("CompatibleNoun");

            StartOfRoundType = AccessTools.TypeByName("StartOfRound");
            SelectableLevelType = AccessTools.TypeByName("SelectableLevel");

            if (TerminalType == null || TerminalNodeType == null || TerminalKeywordType == null || CompatibleNounType == null)
                log.LogWarning("Some terminal types could not be resolved by name. Terminal integration may fail.");

            if (StartOfRoundType == null || SelectableLevelType == null)
                log.LogWarning("Some gameplay types could not be resolved by name. Moon selection may fail.");
        }

        internal static object GetStartOfRoundInstance()
        {
            if (StartOfRoundType == null) return null;

            var prop = StartOfRoundType.GetProperty("Instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (prop != null) return prop.GetValue(null, null);

            var field = StartOfRoundType.GetField("Instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (field != null) return field.GetValue(null);

            return null;
        }
    }
}
