using System;
using System.Collections.Generic;

namespace HoppinHauler.ExtendedRandomMoons
{
    internal static class MoonSelector
    {
        private static readonly System.Random Rand = new System.Random();
        private static readonly Queue<string> LastKeys = new Queue<string>();

        public static bool TryPickMoon(object terminalInstance, ConfigExt cfg, out MoonCandidate chosen, out string failureReason)
        {
            chosen = default(MoonCandidate);
            failureReason = null;

            object sor = ReflectionCache.GetStartOfRoundInstance();
            if (sor == null)
            {
                failureReason = "StartOfRound.Instance not available.";
                return false;
            }

            object[] levels = Util.TryGetLevelsArray(sor);
            if (levels == null || levels.Length == 0)
            {
                failureReason = "No levels found on StartOfRound.";
                return false;
            }

            object currentLevel = Util.TryGetCurrentLevel(sor);
            int groupCredits = Util.TryGetGroupCredits(terminalInstance, int.MaxValue);

            string currentNormName = null;
            int currentLevelId = int.MinValue;
            if (currentLevel != null)
            {
                string curPn = Util.TryGetPlanetName(currentLevel);
                currentNormName = string.IsNullOrEmpty(curPn) ? null : Util.NormalizeMoonName(curPn);
                currentLevelId = Util.TryGetLevelId(currentLevel, int.MinValue);
            }

            ERMLog.Debug("[ERM] TryPickMoon: levels=" + levels.Length +
                         ", current='" + (currentNormName ?? "(null)") + "'" +
                         ", credits=" + (groupCredits == int.MaxValue ? -1 : groupCredits) +
                         ", deduct=" + cfg.DeductCredits.Value +
                         ", diffEachTime=" + cfg.DifferentPlanetEachTime.Value +
                         ", excludeCompany=" + cfg.ExcludeCompanyMoons.Value +
                         ", avoidN=" + cfg.AvoidRepeatCount.Value);

            var candidates = new List<MoonCandidate>(levels.Length);

            for (int i = 0; i < levels.Length; i++)
            {
                object level = levels[i];
                if (level == null) continue;

                string planetName = Util.TryGetPlanetName(level);
                if (string.IsNullOrEmpty(planetName)) continue;

                string normalizedName = Util.NormalizeMoonName(planetName);
                int levelId = Util.TryGetLevelId(level, i);

                if (cfg.DifferentPlanetEachTime.Value)
                {
                    // Best-effort: exclude by levelId if reliable, otherwise by normalized name
                    bool sameId = (currentLevelId != int.MinValue && levelId == currentLevelId);
                    bool sameName = (!string.IsNullOrEmpty(currentNormName) &&
                                     !string.IsNullOrEmpty(normalizedName) &&
                                     string.Equals(normalizedName, currentNormName, StringComparison.InvariantCultureIgnoreCase));

                    if (sameId || sameName)
                    {
                        ERMLog.Debug("[ERM] Filtered (current moon): " + planetName);
                        continue;
                    }
                }

                if (cfg.BlacklistSet.Contains(normalizedName))
                {
                    ERMLog.Debug("[ERM] Filtered (blacklist): " + planetName);
                    continue;
                }

                if (cfg.ExcludeCompanyMoons.Value && Util.IsCompanyMoonHeuristic(level))
                {
                    ERMLog.Debug("[ERM] Filtered (company heuristic): " + planetName);
                    continue;
                }

                string weatherKey = WeatherResolver.GetWeatherKey(level);
                if (!string.IsNullOrEmpty(weatherKey) && cfg.DisallowedWeatherSet.Contains(weatherKey))
                {
                    ERMLog.Debug("[ERM] Filtered (weather '" + weatherKey + "'): " + planetName);
                    continue;
                }

                int cost = -1;
                int cachedCost;
                if (TerminalIntegration.TryGetCachedRouteCost(levelId, out cachedCost))
                    cost = cachedCost;

                var candidate = new MoonCandidate
                {
                    PlanetName = planetName,
                    NormalizedName = normalizedName,
                    LevelId = levelId,
                    Cost = cost,
                    WeatherKey = weatherKey
                };

                ERMLog.Debug("[ERM] Candidate: planet='" + candidate.PlanetName + "', levelId=" + candidate.LevelId +
                             ", cost=" + (candidate.Cost == 0 ? "FREE" : candidate.Cost.ToString()) +
                             ", weather=" + (candidate.WeatherKey ?? "(null)"));

                if (cfg.DeductCredits.Value)
                {
                    if (candidate.Cost > 0 && groupCredits != int.MaxValue && groupCredits < candidate.Cost)
                    {
                        ERMLog.Debug("[ERM] Filtered (cannot afford): " + planetName + " cost=" + candidate.Cost + " credits=" + groupCredits);
                        continue;
                    }
                }

                candidates.Add(candidate);
            }

            ERMLog.Debug("[ERM] Candidates after filtering: " + candidates.Count);

            if (candidates.Count == 0)
            {
                failureReason = "All moons were filtered out.";
                return false;
            }

            int avoidN = cfg.AvoidRepeatCount.Value;
            if (avoidN > 0)
            {
                // Hard cap to prevent pathological values bloating the queue
                if (avoidN > 50) avoidN = 50;

                var filtered = new List<MoonCandidate>(candidates.Count);
                for (int i = 0; i < candidates.Count; i++)
                {
                    if (!LastKeys.Contains(candidates[i].RepeatKey))
                        filtered.Add(candidates[i]);
                }

                if (filtered.Count > 0)
                    candidates = filtered;
            }

            chosen = candidates[Rand.Next(candidates.Count)];

            ERMLog.Debug("[ERM] Chosen: '" + chosen.PlanetName + "' levelId=" + chosen.LevelId +
                         " cost=" + (chosen.Cost == 0 ? "FREE" : chosen.Cost.ToString()) +
                         " weather=" + (string.IsNullOrEmpty(chosen.WeatherKey) ? "(null)" : chosen.WeatherKey));

            if (avoidN > 0)
            {
                LastKeys.Enqueue(chosen.RepeatKey);
                while (LastKeys.Count > avoidN) LastKeys.Dequeue();
            }

            return true;
        }
    }

    internal struct MoonCandidate
    {
        public string PlanetName;
        public string NormalizedName;
        public int Cost;
        public int LevelId;
        public string WeatherKey;

        public string RepeatKey
        {
            get
            {
                return string.IsNullOrEmpty(NormalizedName) ? ("id:" + LevelId) : (NormalizedName + "|id:" + LevelId);
            }
        }
    }
}
