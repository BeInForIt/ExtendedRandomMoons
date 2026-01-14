ExtendedRandomMoons

Adds a terminal command:
- route random

This mod was created based on an idea/request from a member of the Lethal Company Discord community.

The command routes the ship to a random moon while respecting configurable filters:
- blacklist
- disallowed weathers
- exclude company moons (best-effort heuristic)
- avoid repeats
- optionally exclude the currently orbited moon
- optionally filter out moons you cannot afford (when credit deduction is enabled)

Compatibility

- Compatible with LethalLevelLoader (LLL).
- Not compatible with Wesley's Moons and similar progression-lock / gating mods.
  The routing command may still work, but moons locked by progression are NOT filtered out by this mod.

Usage

- In the terminal, type:
  route random

Config

[General]
- DeductCredits (default: true)
  If false, routing via 'route random' will not deduct credits (cost forced to 0).

- SkipConfirmation (default: false)
  If true, 'route random' will skip the confirmation node and route immediately.

- DifferentPlanetEachTime (default: true)
  If true, excludes the currently orbited moon.

- AvoidRepeatCount (default: 3)
  Avoids choosing any of the last N selected moons (best-effort).

[Moons]
- Blacklist (default: Gordion,Liquidation)
  Comma-separated list of moons that will never be selected.

- ExcludeCompanyMoons (default: true)
  If true, excludes company moons (best-effort heuristic).

[Weather]
- DisallowedWeathers (default: Eclipsed)
  Comma-separated list of weather keys/names to exclude.
  Examples:
  Mild,DustClouds,Rainy,Stormy,Foggy,Flooded,Eclipsed

[Debug]
- DebugLogging (default: false)
  Enables verbose debug logs.

Notes

- If all moons are filtered out, the terminal will show a "No suitable moons found" message.
- If DeductCredits is enabled, moons with a route cost higher than your current credits are filtered out.
