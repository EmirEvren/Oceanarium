# ASTRA 0.4 — Cozy fishing, inventory and Oceanarium Journal

This extends the existing Unity project, fishing state machine, eight species definitions, rod attachment, character creator and save path. No external asset packs, replacement fish database, shop, aquarium gameplay or networking were introduced.

## Player-facing changes

- Cozy is the default. Normal and Experienced are selectable in Settings and saved per local collection. `Fishing/DifficultyProfiles.asset` exposes the hook windows, pull, buildup, recovery, break/slack grace, control tolerance, combo mistake grace, stamina, progress and rarity duration targets.
- Hook windows are 3.0 / 2.0 / 1.25 seconds. Cozy applies 0.6× pulling force, 0.5× tension buildup, 1.5× recovery and two seconds of sustained dangerous tension before a break. Regular hooks work; perfect hooks and directional input are bonuses. Release to recover. No random forced escapes.
- Rebuilt fishing panels show large progress/tension bars, numeric percentages and text warnings. Existing bobber, rod-tip, ripple and sound feedback remain. The first-attempt tutorial stops after resolving a catch; completion is saved.
- I opens individual specimen inventory. J opens the journal. The pause menu also opens the journal. These panels block movement/casting and Escape closes them. Sorting supports date, weight, rarity and species; filtering supports actual island and rarity metadata. Repeated species show their owned count without merging specimen identities.
- Specimen details show the saved individual weight, length, quality, catch location/time, estimated value and record status. Valuation uses base value, size and quality; rarity is not multiplied twice.
- Eight Unity-generated color illustrations and matching black silhouettes are connected to the existing species. Unknown species expose only a silhouette and question marks. First genuine catch records discovery immediately, before Keep/Release; releasing retains discovery and records.
- Journal overview, paged species details and records include catch counts, best weight/length, first known discovery, configured habitat/weather/time and discovered islands. Only configured, unlocked island availability appears. The five world island IDs have labels, but unavailable future islands and fish are not invented or exposed.

## Persistence and ownership

The filename remains `fish-collection-v1.json` for compatibility; its internal schema is now version 2. Existing kept specimens, instance/species IDs, measurements and record maxima migrate in place. Writes retain the existing atomic temporary-file/backup approach. Character appearance is a separate file and is not rewritten by these systems.

`FishCollection` separates owned specimens, catch history, discovery/records and resolved decisions. Catch IDs are GUIDs; duplicate events and duplicate retention decisions are rejected. Each local collection owns an identity and preferences. New specimens record owner, UTC capture timestamp, island, zone and rod method. Ownership transitions validate the requesting owner and mutually exclusive Stored/Displayed/Sold/Transferred/Released states. These are preparation interfaces; no aquarium or selling gameplay exists yet.

Legacy version 1 did not save metadata for released fish or exact per-species catch counts. Migration preserves what exists, marks incomplete history and uses explicit unknown dates/count lower bounds. It does not fabricate lost metadata. Corrupt or future-schema documents are retained and write-blocked, with an in-game message, instead of silently being replaced with an empty save. Automatic recovery from a damaged file is not implemented; the existing `.bak` remains available for manual recovery.

## Localization fix and coverage

The existing `LanguageService` now follows Unity's locale-change event, loads a complete translated cache atomically and honors the latest selection during asynchronous loads. Both the in-game language buttons and Unity's top-right locale selector refresh open UI. Selection persists across fresh service initialization and scene changes.

All 17 configured locales are tested against the actual Unity tables and English fallbacks. English and Turkish have complete current keys. The other 15 locales have translated foundational labels but **286 current keys use English fallback**, including existing creator/fishing content and new collection content. Exact key lists are in `ASTRA_04_LOCALIZATION_COVERAGE.txt`. This is not a claim that the whole game has professional translations for all 17 languages.

## Measured validation

- `ASTRA_04_DATA_BALANCE_RESULTS.txt`: 100 deterministic specimens per existing species per difficulty, 2,400 fights total. Cozy uses a regular hook, starter rod, zero directional input and a threshold-based reel/release controller. All eight existing species achieved 100/100 catches in this controlled scenario. Common mean fights are 6.20–6.48 s, Uncommon 9.06–9.45 s, and the current Rare species 13.97 s. Individual results and all three profiles are logged. These are simulation outcomes, not promises about human player success.
- The same focused checks cover sustained-tension grace/recovery, actual profile effects, migration, duplicate prevention, release discovery, sorting/filtering, owner validation, placement exclusion, valuation, preferences and corrupt/future-save protection.
- `ASTRA_04_LIVE_INPUT_RESULTS.txt`: actual Input System keyboard/mouse injection, without assigning progress or forcing success. Deterministic RNG states select legal configured Common and Rare spawns. Sardine was caught in 5.66 s and Golden trevally in 17.37 s, each with a regular hook at 0.85 s and no direction input. Each unlocked its species, updated a personal best and saved exactly one specimen. I/J opened the expected interfaces.
- `ASTRA_01_LOCALIZATION_TEST_RESULTS.txt`: all current keys across 17 locales, external locale selection, rapid selections, an external change during loading, unknown-locale fallback, saved Turkish restoration and menu/home transitions passed.
- `ASTRA_04_REGRESSION_RESULTS.txt`: separate Play Mode restart, saved collection/appearance restoration, movement/sprint/camera, modal shortcuts, scene transitions and references. See the result file for the executed outcome and editor UI sample.

## Scope limits

Only Common, Uncommon and Rare species exist in the current eight-species content. Epic/Legendary duration settings are prepared, but no invented Epic/Legendary species or success-rate claims were added. Island unlock state is ready for later progression rules; current actual availability is Home Island. All networking structures are local preparation only: FishNet/Steamworks authority and transport are neither implemented nor tested. Large inventories, standalone builds and multiplayer memory/performance still need later profiling.

## Tools and files

Editor commands under `Oceanarium Fishing`: Build ASTRA 0.4 Collection, Run ASTRA 0.4 Data and Balance Checks, Run Live Language Checks. The existing local editor bridge also exposes `cozy-demo` and `cozy-runtime-checks` for reproducible checks. Asset generation creates or updates only the targeted profiles, icons, existing player prefab and localization tables.

New runtime scripts: `FishingDifficultyProfiles.cs`, `CollectionHUD.cs`, `CozyUI.cs`. New editor scripts: `CozyFishingBuilder.cs`, `CozyFishingChecks.cs`, `CozyFishingDemo.cs`, `CozyRuntimeChecks.cs`. Modified runtime systems: `FishingController`, `FishingFight`, `FishingTuning`, `FishSpecies`, `FishInstance`/`FishCollection`/`FishCollectionStore`, `FishingHUD`, `FoundationHUD`, `LanguageService`. Exact working-tree paths are in `ASTRA_04_FILE_INVENTORY.md`.

Real Unity captures are stored in `Docs/Screenshots/ASTRA_04_*`. Work stops at ASTRA 0.4.

## Final restart and UI verification

The separate Play Mode restart/regression run passed. Inventory, history, records and preferences reloaded; duplicate catch IDs were absent. WASD, sprint, RMB orbit, inventory input blocking, Escape closing without opening pause, MainMenu/CharacterCreator/HomeIsland transitions and supported scene materials passed. Character save bytes and collection save bytes remained unchanged by those transitions.

The journal's 120-frame editor sample averaged 5.45 ms per frame, with a 1,585,152-byte managed heap increase over the sample (engine/editor activity included). This is neither an isolated allocation rate nor a standalone performance guarantee.

Final panel backgrounds use fixed-size corners, and the eight illustrations have distinct body/tail/fin proportions. Home Island remains open in Play Mode. No ASTRA 0.5 work was started.

## Final screenshots and live language check

- [Individual inventory](Screenshots/ASTRA_04_Inventory_Final.png)
- [The same open inventory after selecting Turkish in Unity](Screenshots/ASTRA_04_Inventory_Turkish.png)
- [Unknown species silhouettes](Screenshots/ASTRA_04_Journal_Locked_Final.png)
- [Discovered Rare species and its saved records](Screenshots/ASTRA_04_Journal_Final.png)
- [Personal records](Screenshots/ASTRA_04_Records_Final.png)

These final 1920×1080 Game View captures include the corrected panel corners and text contrast. Selecting `tr` through Unity's top-right dropdown updated the already-open inventory immediately. Turkish is left selected as the saved user preference; a new profile with no saved locale still defaults to English. No new compile/runtime errors appeared after the final Play Mode start.
