# ASTRA 0.9 integration audit

Existing runtime time sources: TownWorld owns TownClock (2 minutes/second, saved only in town); FishingController passes noon/clear to FishingZone; AquariumWorld has fixed noon. Boat travel uses real seconds only for cinematics, which should remain real time. Fishing waits, buffs, animation and arcade timing are real-time durations, not calendar clocks.

Canonical progress is FishCollection, with atomic copy/write/swap through FishingController.CommitAquarium. Add world schema independently; migrate legacy town elapsedMinutes once. Never calculate offline income. Persist snapshots before scene changes, and use a session-scoped clock so destination loading does not reset it. Shop daily limits and quests remain player-owned; clock and forecasts are future host-owned.

Presentation: existing OF_TropicalSky, OF_CozyOcean, OF_Foliage and OF_IslandBlend shaders; no duplicate ocean. FishingWater must share weather wave multiplier with the shader. Seasonal colors should use global shader values or property blocks. Weather definitions and calendar settings are editable assets. Deterministic schedules derive from saved seed/day/block and season; UI uses those same schedules.

Calendar target: 4 months x 28 days, 112-day year, Monday index 0, default 08:00 and one minute per real second. Preserve prior simulated day numbers when migrating town saves. Sleep must advance to next day's morning, flush canonical save, clear visitor sessions without paying skipped time and restore input.
