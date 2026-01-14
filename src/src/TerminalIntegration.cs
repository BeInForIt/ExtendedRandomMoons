using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;

namespace HoppinHauler.ExtendedRandomMoons
{
    internal static class TerminalIntegration
    {
        private const string NodeNameRouteRandom = "erm_routeRandom";
        private const string NodeNameNoSuitable = "erm_noSuitable";

        private static ManualLogSource _log;

        private static object _routeKeyword;
        private static object _sampleDenyCompatibleNoun;
        private static object _sampleConfirmKeyword;

        private static readonly Dictionary<int, int> _routeCostByLevelId = new Dictionary<int, int>();

        public static bool TryGetCachedRouteCost(int levelId, out int cost)
        {
            return _routeCostByLevelId.TryGetValue(levelId, out cost);
        }

        public static void ApplyPatches(Harmony harmony, ManualLogSource log)
        {
            _log = log;

            var terminalAwake = AccessTools.Method(ReflectionCache.TerminalType, "Awake");
            var parseSentence = AccessTools.Method(ReflectionCache.TerminalType, "ParsePlayerSentence");

            if (terminalAwake != null)
                harmony.Patch(terminalAwake, postfix: new HarmonyMethod(typeof(TerminalIntegration), nameof(TerminalAwakePostfix)));

            if (parseSentence != null)
                harmony.Patch(parseSentence, postfix: new HarmonyMethod(typeof(TerminalIntegration), nameof(ParsePlayerSentencePostfix)));
        }

        public static void TerminalAwakePostfix(object __instance)
        {
            try
            {
                if (__instance == null) return;

                Plugin.Cfg.ReloadDerived();

                _routeKeyword = TerminalNodeFactory.GetKeywordByName(__instance, "Route");
                if (_routeKeyword == null)
                {
                    _log.LogWarning("Failed to find Route keyword.");
                    return;
                }

                // Always rebuild cost cache (other mods may change route catalog over time)
                BuildRouteCostCacheFromRouteKeyword(_routeKeyword);

                // Prevent double-registration (scene reloads / repeated Terminal.Awake / other patches)
                if (TerminalNodeFactory.HasRouteRandomAlready(__instance))
                {
                    ERMLog.Debug("[ERM] route random already registered, skipping.");
                    TerminalNodeFactory.TryCacheConfirmDenySamples(__instance, _routeKeyword, out _sampleConfirmKeyword, out _sampleDenyCompatibleNoun);
                    return;
                }

                // Templates for LLL compatibility: clone a real route noun keyword + compatible noun
                object templateNounKeyword = null;
                object templateCompatibleNoun = null;

                Array routeCompatibleNouns = Util.TryGetMemberValue(_routeKeyword, "compatibleNouns") as Array;
                if (routeCompatibleNouns != null)
                {
                    for (int i = 0; i < routeCompatibleNouns.Length; i++)
                    {
                        object cn = routeCompatibleNouns.GetValue(i);
                        if (cn == null) continue;

                        object noun = Util.TryGetMemberValue(cn, "noun");
                        if (noun == null) continue;

                        templateCompatibleNoun = cn;
                        templateNounKeyword = noun;
                        break;
                    }
                }

                object randomKeyword = TerminalNodeFactory.CreateTerminalKeywordFromTemplate(
                    templateKeyword: templateNounKeyword,
                    word: "random",
                    name: "ExtendedRandom",
                    defaultVerbKeyword: _routeKeyword);

                object resultNode = TerminalNodeFactory.CreateEmptyResultNode(NodeNameRouteRandom);

                object compatibleNoun = TerminalNodeFactory.CreateCompatibleNounFromTemplate(
                    templateCompatibleNoun: templateCompatibleNoun,
                    nounKeyword: randomKeyword,
                    resultNode: resultNode);

                TerminalNodeFactory.AddKeyword(__instance, randomKeyword);
                TerminalNodeFactory.AddCompatibleNounToKeyword(__instance, "Route", compatibleNoun);

                TerminalNodeFactory.AppendMoonsHelp(__instance,
                    "* Random   //   Routes you to a random moon (uses config filters)\n");

                TerminalNodeFactory.TryCacheConfirmDenySamples(__instance, _routeKeyword, out _sampleConfirmKeyword, out _sampleDenyCompatibleNoun);

                _log.LogInfo("Registered terminal command: route random");
            }
            catch (Exception e)
            {
                _log.LogError("Failed to install terminal keywords.");
                _log.LogError(e);
            }
        }

        public static void ParsePlayerSentencePostfix(ref object __result, object __instance)
        {
            try
            {
                if (__result == null || __instance == null) return;

                string resultName = TerminalNodeFactory.TryGetNodeName(__result);
                if (!string.Equals(resultName, NodeNameRouteRandom, StringComparison.InvariantCultureIgnoreCase))
                    return;

                Plugin.Cfg.ReloadDerived();

                MoonCandidate chosen;
                string failureReason;
                if (!MoonSelector.TryPickMoon(__instance, Plugin.Cfg, out chosen, out failureReason))
                {
                    __result = TerminalNodeFactory.CreateSimpleNode(
                        name: NodeNameNoSuitable,
                        displayText: "\nNo suitable moons found.\n" +
                                     "Check your ExtendedRandomMoons config (blacklist/weather/cost).\n" +
                                     (string.IsNullOrEmpty(failureReason) ? "\n\n\n" : ("\n" + failureReason + "\n\n\n")),
                        clearPreviousText: true);
                    return;
                }

                bool free = !Plugin.Cfg.DeductCredits.Value;

                object routeNode = TerminalNodeFactory.CreateRouteNode(
                    terminalInstance: __instance,
                    moon: chosen,
                    free: free,
                    skipConfirmation: Plugin.Cfg.SkipConfirmation.Value,
                    sampleConfirmKeyword: _sampleConfirmKeyword,
                    sampleDenyCompatibleNoun: _sampleDenyCompatibleNoun);

                __result = routeNode;
            }
            catch (Exception e)
            {
                _log.LogError("Failed handling route random.");
                _log.LogError(e);
            }
        }

        private static void BuildRouteCostCacheFromRouteKeyword(object routeKeyword)
        {
            _routeCostByLevelId.Clear();

            object sor = ReflectionCache.GetStartOfRoundInstance();
            object[] levels = sor != null ? Util.TryGetLevelsArray(sor) : null;

            Array routeCompatibleNouns = Util.TryGetMemberValue(routeKeyword, "compatibleNouns") as Array;
            if (routeCompatibleNouns == null || routeCompatibleNouns.Length == 0)
            {
                ERMLog.Debug("[ERM] Route cost cache: route keyword has no compatibleNouns.");
                return;
            }

            for (int i = 0; i < routeCompatibleNouns.Length; i++)
            {
                object cn = routeCompatibleNouns.GetValue(i);
                if (cn == null) continue;

                object noun = Util.TryGetMemberValue(cn, "noun");
                object result = Util.TryGetMemberValue(cn, "result");
                if (noun == null || result == null) continue;

                int itemCost = Util.TryGetIntMember(result, "itemCost", int.MinValue);
                if (itemCost == int.MinValue) itemCost = Util.TryGetIntMember(result, "ItemCost", int.MinValue);
                if (itemCost == int.MinValue) continue;

                string nounName = Util.TryGetStringMember(noun, "name") ?? Util.TryGetStringMember(noun, "word");
                if (string.IsNullOrEmpty(nounName)) continue;

                int levelId = FindLevelIdByName(levels, nounName);

                if (levelId < 0)
                {
                    int brtm = Util.TryGetIntMember(result, "buyRerouteToMoon", int.MinValue);
                    if (brtm != int.MinValue && brtm >= 0)
                        levelId = brtm;
                }

                if (levelId < 0) continue;

                _routeCostByLevelId[levelId] = itemCost;
            }

            ERMLog.Debug("[ERM] Route cost cache built: entries=" + _routeCostByLevelId.Count);
        }

        private static int FindLevelIdByName(object[] levels, string nounName)
        {
            if (levels == null || levels.Length == 0) return -1;

            string normNoun = Util.NormalizeMoonName(nounName);

            for (int i = 0; i < levels.Length; i++)
            {
                object level = levels[i];
                if (level == null) continue;

                string planet = Util.TryGetPlanetName(level);
                if (string.IsNullOrEmpty(planet)) continue;

                string normPlanet = Util.NormalizeMoonName(planet);

                if (string.Equals(normPlanet, normNoun, StringComparison.InvariantCultureIgnoreCase) ||
                    normPlanet.IndexOf(normNoun, StringComparison.InvariantCultureIgnoreCase) >= 0 ||
                    normNoun.IndexOf(normPlanet, StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    return Util.TryGetLevelId(level, i);
                }
            }

            return -1;
        }
    }
}
