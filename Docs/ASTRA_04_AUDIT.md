# ASTRA 0.4 implementation audit

The existing ASTRA 0.3 fishing controller/state machine, eight FishSpecies assets, zone configuration, seeded FishInstance generation, presentation, rod attachment and fish-collection-v1.json save path are retained.

Difficulty issues in the original simulation:
- Hook window 1.8 s; perfect window 0.48 s.
- Directional control falls to 0.35 with neutral mouse input. Progress and stamina drain require control >0.48, blocking progress during part of each pull cycle.
- Tension rises at 0.20 × fish force × surge × directional penalty; recovery 0.32/s; break grace 1.2 s and slack escape 6 s.
- Fish strength/stamina and specimen size combine with mandatory directional accuracy, stretching fights beyond the intended casual duration.
- Recovery increases combo but any off-control frame resets clean reeling time.
- No random forced escapes were present; preserve that property.

Existing data: version 1 contains kept specimens, best weight/length per species and resolved instance IDs. Released specimen metadata and per-species catch counts were not saved, so migration cannot invent those details. Preserve IDs, owned specimens, record maxima and known discoveries; report incomplete legacy counts/dates explicitly. Character appearance uses a separate file and will not be rewritten.

Localization fix already applied to source: subscribe to Unity locale selection, atomically publish loaded tables, keep the latest pending selection, fill 17 foundational UI tables. Existing fishing and creator keys remain in the same OF_UI table; new milestone strings use English/Turkish with English fallback where not translated.
