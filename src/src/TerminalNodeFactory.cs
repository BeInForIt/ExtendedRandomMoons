using System;
using UnityEngine;

namespace HoppinHauler.ExtendedRandomMoons
{
    internal static class TerminalNodeFactory
    {
        private static object SmartCreateInstance(Type t)
        {
            if (t == null) return null;

            if (typeof(ScriptableObject).IsAssignableFrom(t))
                return ScriptableObject.CreateInstance(t);

            return Activator.CreateInstance(t);
        }

        private static object CloneScriptableObject(object template)
        {
            if (template == null) return null;

            ScriptableObject so = template as ScriptableObject;
            if (so == null) return null;

            return UnityEngine.Object.Instantiate(so);
        }

        public static object GetKeywordByName(object terminalInstance, string keywordName)
        {
            if (terminalInstance == null) return null;

            object terminalNodes = Util.TryGetMemberValue(terminalInstance, "terminalNodes");
            if (terminalNodes == null) return null;

            Array allKeywords = Util.TryGetMemberValue(terminalNodes, "allKeywords") as Array;
            if (allKeywords == null) return null;

            for (int i = 0; i < allKeywords.Length; i++)
            {
                object kw = allKeywords.GetValue(i);
                if (kw == null) continue;

                string name = Util.TryGetStringMember(kw, "name");
                if (string.Equals(name, keywordName, StringComparison.InvariantCultureIgnoreCase))
                    return kw;
            }

            return null;
        }

        public static object GetKeywordByWord(object terminalInstance, string word)
        {
            if (terminalInstance == null) return null;

            object terminalNodes = Util.TryGetMemberValue(terminalInstance, "terminalNodes");
            if (terminalNodes == null) return null;

            Array allKeywords = Util.TryGetMemberValue(terminalNodes, "allKeywords") as Array;
            if (allKeywords == null) return null;

            for (int i = 0; i < allKeywords.Length; i++)
            {
                object kw = allKeywords.GetValue(i);
                if (kw == null) continue;

                string w = Util.TryGetStringMember(kw, "word");
                if (string.Equals(w, word, StringComparison.InvariantCultureIgnoreCase))
                    return kw;
            }

            return null;
        }

        public static bool HasRouteRandomAlready(object terminalInstance)
        {
            object randomKw = GetKeywordByWord(terminalInstance, "random");
            if (randomKw == null) return false;

            object routeKw = GetKeywordByName(terminalInstance, "Route");
            if (routeKw == null) return false;

            Array routeCompatibleNouns = Util.TryGetMemberValue(routeKw, "compatibleNouns") as Array;
            if (routeCompatibleNouns == null) return false;

            for (int i = 0; i < routeCompatibleNouns.Length; i++)
            {
                object cn = routeCompatibleNouns.GetValue(i);
                if (cn == null) continue;

                object noun = Util.TryGetMemberValue(cn, "noun");
                if (noun == null) continue;

                if (ReferenceEquals(noun, randomKw))
                    return true;

                string nounWord = Util.TryGetStringMember(noun, "word");
                if (string.Equals(nounWord, "random", StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }

            return false;
        }

        public static void AddKeyword(object terminalInstance, object newKeyword)
        {
            object terminalNodes = Util.TryGetMemberValue(terminalInstance, "terminalNodes");
            if (terminalNodes == null) return;

            Array allKeywords = Util.TryGetMemberValue(terminalNodes, "allKeywords") as Array;
            if (allKeywords == null) return;

            Array appended = Util.AppendToArray(allKeywords, newKeyword);
            Util.TrySetMemberValue(terminalNodes, "allKeywords", appended);
        }

        public static void AddCompatibleNounToKeyword(object terminalInstance, string keywordName, object newCompatibleNoun)
        {
            object kw = GetKeywordByName(terminalInstance, keywordName);
            if (kw == null) return;

            Array compatibleNouns = Util.TryGetMemberValue(kw, "compatibleNouns") as Array;
            if (compatibleNouns == null)
                compatibleNouns = Array.CreateInstance(ReflectionCache.CompatibleNounType, 0);

            Array appended = Util.AppendToArray(compatibleNouns, newCompatibleNoun);
            Util.TrySetMemberValue(kw, "compatibleNouns", appended);
        }

        public static void AppendMoonsHelp(object terminalInstance, string extraText)
        {
            object moonsKeyword = GetKeywordByName(terminalInstance, "Moons");
            if (moonsKeyword == null) return;

            object specialKeywordResult = Util.TryGetMemberValue(moonsKeyword, "specialKeywordResult");
            if (specialKeywordResult == null) return;

            string displayText = Util.TryGetStringMember(specialKeywordResult, "displayText") ?? "";

            if (displayText.IndexOf(extraText, StringComparison.InvariantCultureIgnoreCase) >= 0)
                return;

            if (extraText.IndexOf("* Random", StringComparison.InvariantCultureIgnoreCase) >= 0 &&
                displayText.IndexOf("* Random", StringComparison.InvariantCultureIgnoreCase) >= 0)
                return;

            displayText += extraText;
            Util.TrySetMemberValue(specialKeywordResult, "displayText", displayText);
        }

        public static object CreateTerminalKeywordFromTemplate(object templateKeyword, string word, string name, object defaultVerbKeyword)
        {
            object kw = CloneScriptableObject(templateKeyword) ?? SmartCreateInstance(ReflectionCache.TerminalKeywordType);

            Util.TrySetMemberValue(kw, "word", word);
            Util.TrySetMemberValue(kw, "name", name);
            Util.TrySetMemberValue(kw, "defaultVerb", defaultVerbKeyword);

            Util.TrySetMemberValue(kw, "compatibleNouns", Array.CreateInstance(ReflectionCache.CompatibleNounType, 0));

            // Best-effort compatibility flags (some patches read these)
            Util.TrySetMemberValue(kw, "isVerb", false);
            Util.TrySetMemberValue(kw, "IsVerb", false);

            return kw;
        }

        public static object CreateCompatibleNounFromTemplate(object templateCompatibleNoun, object nounKeyword, object resultNode)
        {
            object cn = CloneScriptableObject(templateCompatibleNoun) ?? SmartCreateInstance(ReflectionCache.CompatibleNounType);

            Util.TrySetMemberValue(cn, "noun", nounKeyword);
            Util.TrySetMemberValue(cn, "result", resultNode);
            return cn;
        }

        public static object CreateEmptyResultNode(string name)
        {
            object node = SmartCreateInstance(ReflectionCache.TerminalNodeType);
            Util.TrySetMemberValue(node, "name", name);
            Util.TrySetMemberValue(node, "buyRerouteToMoon", -1);
            Util.TrySetMemberValue(node, "terminalOptions", Array.CreateInstance(ReflectionCache.CompatibleNounType, 0));
            return node;
        }

        public static object CreateSimpleNode(string name, string displayText, bool clearPreviousText)
        {
            object node = SmartCreateInstance(ReflectionCache.TerminalNodeType);
            Util.TrySetMemberValue(node, "name", name);
            Util.TrySetMemberValue(node, "displayText", displayText);
            Util.TrySetMemberValue(node, "clearPreviousText", clearPreviousText);
            return node;
        }

        public static string TryGetNodeName(object terminalNode)
        {
            return Util.TryGetStringMember(terminalNode, "name");
        }

        public static void TryCacheConfirmDenySamples(object terminalInstance, object routeKeyword, out object confirmKeyword, out object denyCompatibleNoun)
        {
            confirmKeyword = GetKeywordByName(terminalInstance, "Confirm");
            denyCompatibleNoun = null;

            Array routeCompatibleNouns = Util.TryGetMemberValue(routeKeyword, "compatibleNouns") as Array;
            if (routeCompatibleNouns == null) return;

            for (int i = 0; i < routeCompatibleNouns.Length; i++)
            {
                object cn = routeCompatibleNouns.GetValue(i);
                if (cn == null) continue;

                object resultNode = Util.TryGetMemberValue(cn, "result");
                if (resultNode == null) continue;

                int buyRerouteToMoon = Util.TryGetIntMember(resultNode, "buyRerouteToMoon", int.MinValue);
                if (buyRerouteToMoon != -2) continue;

                Array options = Util.TryGetMemberValue(resultNode, "terminalOptions") as Array;
                if (options == null) continue;

                for (int j = 0; j < options.Length; j++)
                {
                    object opt = options.GetValue(j);
                    if (opt == null) continue;

                    object noun = Util.TryGetMemberValue(opt, "noun");
                    if (noun == null) continue;

                    string nounName = Util.TryGetStringMember(noun, "name");
                    if (string.Equals(nounName, "Deny", StringComparison.InvariantCultureIgnoreCase))
                    {
                        denyCompatibleNoun = opt;
                        return;
                    }
                }
            }
        }

        public static object CreateRouteNode(
            object terminalInstance,
            MoonCandidate moon,
            bool free,
            bool skipConfirmation,
            object sampleConfirmKeyword,
            object sampleDenyCompatibleNoun)
        {
            object confirmNode = SmartCreateInstance(ReflectionCache.TerminalNodeType);
            Util.TrySetMemberValue(confirmNode, "name", "erm_confirmRoute_" + Util.SanitizeId(moon.NormalizedName));
            Util.TrySetMemberValue(confirmNode, "buyRerouteToMoon", moon.LevelId);
            Util.TrySetMemberValue(confirmNode, "clearPreviousText", true);

            int itemCost = free ? 0 : (moon.Cost >= 0 ? moon.Cost : 0);
            string planetTextName = moon.PlanetName ?? moon.NormalizedName ?? "UNKNOWN";
            string costText = itemCost == 0 ? "FREE" : (itemCost + " credits");

            ERMLog.Debug("[ERM] CreateRouteNode: planet='" + planetTextName +
                         "' levelId=" + moon.LevelId +
                         " cost=" + (moon.Cost == 0 ? "FREE" : moon.Cost.ToString()) +
                         " freeFlag=" + free +
                         " itemCost=" + (itemCost == 0 ? "FREE" : itemCost.ToString()) +
                         " skipConfirm=" + skipConfirmation);

            Util.TrySetMemberValue(confirmNode, "itemCost", itemCost);

            string display = "\nRouting autopilot to " + planetTextName + " (" + costText + ").\n" +
                             "Your new balance is [playerCredits].\n\nPlease enjoy your flight.\n\n\n";
            Util.TrySetMemberValue(confirmNode, "displayText", display);

            if (skipConfirmation)
                return confirmNode;

            object routeNode = SmartCreateInstance(ReflectionCache.TerminalNodeType);
            Util.TrySetMemberValue(routeNode, "name", "erm_routeNode_" + Util.SanitizeId(moon.NormalizedName));
            Util.TrySetMemberValue(routeNode, "buyRerouteToMoon", -2);
            Util.TrySetMemberValue(routeNode, "clearPreviousText", true);
            Util.TrySetMemberValue(routeNode, "overrideOptions", true);
            Util.TrySetMemberValue(routeNode, "displayPlanetInfo", true);

            string prompt = "\nRoute autopilot to " + planetTextName + " (" + costText + ")?\n" +
                            "Type 'c' to confirm or 'd' to cancel.\n\n";
            Util.TrySetMemberValue(routeNode, "displayText", prompt);

            // Clone from deny option (template) to satisfy mod validators
            object confirmCompatibleNoun = CreateCompatibleNounFromTemplate(sampleDenyCompatibleNoun, sampleConfirmKeyword, confirmNode);

            object denyOpt = sampleDenyCompatibleNoun;
            if (denyOpt == null)
            {
                object denyNode = CreateSimpleNode("erm_deny", "\nCancelled.\n\n\n", true);
                object denyKeyword = GetKeywordByName(terminalInstance, "Deny");
                denyOpt = CreateCompatibleNounFromTemplate(null, denyKeyword, denyNode);
            }

            Array optionsArr = Array.CreateInstance(ReflectionCache.CompatibleNounType, 2);
            optionsArr.SetValue(denyOpt, 0);
            optionsArr.SetValue(confirmCompatibleNoun, 1);
            Util.TrySetMemberValue(routeNode, "terminalOptions", optionsArr);

            return routeNode;
        }
    }
}
