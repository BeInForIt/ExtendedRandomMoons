using System;

namespace HoppinHauler.ExtendedRandomMoons
{
    internal static class WeatherResolver
    {
        public static string GetWeatherKey(object selectableLevel)
        {
            if (selectableLevel == null) return null;

            object weather = Util.TryGetMemberValue(selectableLevel, "currentWeather");
            if (weather != null)
            {
                string key = NormalizeWeather(weather);
                if (!string.IsNullOrEmpty(key)) return key;
            }

            object weatherStr = Util.TryGetMemberValue(selectableLevel, "currentWeatherString")
                                ?? Util.TryGetMemberValue(selectableLevel, "weatherString")
                                ?? Util.TryGetMemberValue(selectableLevel, "Weather")
                                ?? Util.TryGetMemberValue(selectableLevel, "weather");
            if (weatherStr is string s && !string.IsNullOrWhiteSpace(s))
            {
                return Util.NormalizeWeatherToken(s);
            }

            return null;
        }

        private static string NormalizeWeather(object weatherValue)
        {
            string raw = weatherValue.ToString();
            if (string.IsNullOrWhiteSpace(raw)) return null;

            if (string.Equals(raw, "None", StringComparison.InvariantCultureIgnoreCase))
                return "mild";

            return Util.NormalizeWeatherToken(raw);
        }
    }
}
