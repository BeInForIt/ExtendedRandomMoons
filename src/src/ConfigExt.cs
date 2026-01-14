using BepInEx.Configuration;
using System;
using System.Collections.Generic;

namespace HoppinHauler.ExtendedRandomMoons
{
    internal sealed class ConfigExt
    {
        private readonly ConfigFile _cfg;

        public ConfigEntry<bool> DeductCredits;
        public ConfigEntry<bool> SkipConfirmation;
        public ConfigEntry<bool> DifferentPlanetEachTime;
        public ConfigEntry<int> AvoidRepeatCount;

        public ConfigEntry<string> Blacklist;
        public ConfigEntry<bool> ExcludeCompanyMoons;

        public ConfigEntry<string> DisallowedWeathers;

        public ConfigEntry<bool> DebugLogging;

        public readonly HashSet<string> BlacklistSet = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
        public readonly HashSet<string> DisallowedWeatherSet = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

        public ConfigExt(ConfigFile cfg)
        {
            _cfg = cfg;
            _cfg.SaveOnConfigSet = false;

            DeductCredits = _cfg.Bind("General", "DeductCredits", true, "If false, routing via 'route random' will not deduct credits (cost forced to 0).");
            SkipConfirmation = _cfg.Bind("General", "SkipConfirmation", false, "If true, 'route random' will skip the confirmation node and route immediately.");
            DifferentPlanetEachTime = _cfg.Bind("General", "DifferentPlanetEachTime", true, "If true, excludes the currently orbited moon.");
            AvoidRepeatCount = _cfg.Bind("General", "AvoidRepeatCount", 3, "Avoids choosing any of the last N selected moons (best-effort).");

            Blacklist = _cfg.Bind("Moons", "Blacklist", "Gordion,Liquidation", "Comma-separated list of moons that will never be selected (e.g. Gordion,Liquidation).");
            ExcludeCompanyMoons = _cfg.Bind("Moons", "ExcludeCompanyMoons", true, "If true, excludes company moons (best-effort heuristic).");

            DisallowedWeathers = _cfg.Bind("Weather", "DisallowedWeathers", "Eclipsed", "Comma-separated list of weather keys/names to exclude. Vanilla examples: Mild,DustClouds,Rainy,Stormy,Foggy,Flooded,Eclipsed");

            DebugLogging = _cfg.Bind("Debug", "DebugLogging", false, "If true, enables verbose [ERM] debug logs.");

            _cfg.Save();
            _cfg.SaveOnConfigSet = true;
        }

        public void ReloadDerived()
        {
            Util.ParseCsvToNormalizedSet(Blacklist.Value, BlacklistSet);
            Util.ParseCsvToNormalizedSet(DisallowedWeathers.Value, DisallowedWeatherSet);
            Util.ExpandWeatherAliases(DisallowedWeatherSet);
        }
    }
}
