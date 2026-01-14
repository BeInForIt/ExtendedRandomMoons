namespace HoppinHauler.ExtendedRandomMoons
{
    internal static class ERMLog
    {
        public static void Debug(string message)
        {
            if (Plugin.Log == null) return;
            if (Plugin.Cfg == null || Plugin.Cfg.DebugLogging == null || !Plugin.Cfg.DebugLogging.Value) return;
            Plugin.Log.LogInfo(message);
        }

        public static void Info(string message)
        {
            if (Plugin.Log == null) return;
            Plugin.Log.LogInfo(message);
        }

        public static void Warn(string message)
        {
            if (Plugin.Log == null) return;
            Plugin.Log.LogWarning(message);
        }

        public static void Error(string message)
        {
            if (Plugin.Log == null) return;
            Plugin.Log.LogError(message);
        }
    }
}
