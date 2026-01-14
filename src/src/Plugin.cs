using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;

namespace HoppinHauler.ExtendedRandomMoons
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "HoppinHauler.extendedrandommoons";
        public const string PluginName = "ExtendedRandomMoons";
        public const string PluginVersion = "1.0.0";

        internal static ManualLogSource Log;
        internal static ConfigExt Cfg;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            try
            {
                Cfg = new ConfigExt(Config);
                Cfg.ReloadDerived();

                ReflectionCache.Warmup(Log);

                _harmony = new Harmony(PluginGuid);
                TerminalIntegration.ApplyPatches(_harmony, Log);

                Log.LogInfo(PluginName + " loaded.");
            }
            catch (Exception e)
            {
                Logger.LogError("Failed to initialize " + PluginName);
                Logger.LogError(e);
            }
        }
    }
}
