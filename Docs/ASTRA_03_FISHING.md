# ASTRA 0.3 — Foundational fishing

## Play

Enter OF_HomeIsland. Collect one of the four rods with E at the pier. Hold right mouse to face open water. Hold left mouse to charge; release to cast. The green landing ring means valid water. The amber power band rewards timing with accurate placement and a shorter bite wait. Click left mouse when the bobber bites. During the fight, hold left mouse to reel, release to lower tension, and move the mouse against the fish's pull. Match the gold steering marker to the green target. Q cancels; the catch card offers Keep and Release.

## Components and assets

- FishingRodDefinition: ScriptableObject equipment stats, rarity, upgrade and future durability settings. `Fishing/StarterRod.asset` and `OF_EquippedStarterRod.prefab` provide the starter equipment.
- FishSpecies: species definition, ranges, habitat/time/weather/bait eligibility, rarity, behavior, stamina and future aquarium/encyclopedia hooks.
- FishingTuning: centralized charge, waiting, reaction, tension, stamina, progress and combo parameters in `Fishing/CozyFishingTuning.asset`.
- FishingZone: four Home Island spawn distributions, filtered against species eligibility. Beach, Rocky, Pier and Deep. Current environment inputs are explicitly daytime 12:00, clear weather, no bait.
- FishingData: seeded FishInstance generation and version 1 FishCollection persistence, separate from appearance JSON. Keep and Release both update discovery/personal records; only Keep adds ownership. Duplicate instance resolution is rejected. Saves stage a copy, write a temporary file and replace the old file with a backup. File: `Application.persistentDataPath/fish-collection-v1.json`.
- FishingController: per-player input and validated transitions; accepts independent externally supplied commands for testing/future host integration. Existing movement and interactions consult FishingLocked while the regular settings/input gate remains separate.
- FishingSimulation / FishingFight: independent fight calculation, pull archetypes, weight/difficulty, tension, stamina, progress and capped skill combos. No globally shared active fish.
- FishingWater: CPU sampling of the existing ocean shader's two sine displacement terms, using the same material parameters and time. Base ocean height is 0.05 m. Cast validation checks zone coverage and solid terrain above water.
- FishingPresentation / FishingRipple: hand-attached rod, curved line, bobber, ripples, procedural fish reveal, live skeletal poses and synthesized cues. No purchased/downloaded audio or models. Audio is deliberately a placeholder synthesis, not recorded realistic sound.
- FishingHUD: localized charge, bite, fight and result UI.
- FishingBuilder, FishingChecks, FishingInputDemo: editor-only asset setup, focused validation and actual Input System device demonstration.

Species: clownfish, blue tang, sardine, mackerel, red snapper, sea bream, pufferfish and golden trevally. Rarity tiers are Common, Uncommon, Rare, Epic and Legendary; starter content uses the first three. Rarity and specimen size quality are separate. Weight follows a cubic relationship to seeded length with bounded individual variation. Large specimens are less frequent.

## State machine

Idle → Equipping → Idle → Aiming → Casting → BobberFlying → WaitingForBite → FishBiting → Hooked → Fighting ↔ Reeling → CatchSuccess → ReturningToIdle → Idle.

Missed bites, overload or prolonged slack lead to FishEscaped, then cleanup. Cancellation returns through ReturningToIdle. CatchSuccess remains until a successful Keep/Release write; unsuccessful saving leaves the result available for retry. New casts and duplicate equipment are rejected while already active. Scene destruction removes cast visuals and releases the movement lock.

## Fight model

Calm pulls slowly; Darting changes direction quickly; Heavy increases force; Erratic combines oscillations; Stubborn uses the high-resistance stamina profile. Reeling increases tension. Correct counter-steering in the safe tension range drains stamina and advances the catch. Releasing recovers tension but slowly loses progress while the fish is energetic. Overload and prolonged slack can lose the fish. Perfect hooks reduce initial stamina modestly. Sustained controlled reeling and deliberate tension recovery earn combos, capped at five with small additive bonuses. Catch success requires both exhausted stamina and full progress.

## Integration boundaries

The existing avatar, controller, character creator and scene flow remain in place. Home Island adds only fishing habitat configuration; island art is not rebuilt. Rod pickup state is session-only; fish ownership persists. Rod durability is configuration-only for future use.

Future host authority must validate commands, run species/specimen rolls and award transactions. DTOs contain stable species IDs, seed, dimensions, quality, time and location rather than mesh references. No FishNet synchronization or Steam authentication is implemented. Aquarium, selling, inventory UI and full encyclopedia are outside this milestone.

## Localization and validation

Fishing keys are added to the existing OF_UI tables. English and Turkish are supplied; the other 15 locales intentionally fall back to English for new fishing strings. Existing translations are preserved. Reports: ASTRA_03_TEST_RESULTS.txt and ASTRA_03_INPUT_RESULTS.txt. These distinguish direct deterministic simulation checks from actual Input System-driven Play Mode behavior.

Standalone executable restart, multiplayer synchronization, exhaustive cosmetic combinations and final production audio are not certified by editor tests. Local fish-file reload is tested independently from process restart.

## Verified delivery — 17 October 2026

Fishing checks passed at 07:35 UTC, including all eight species, habitat selection, valid/invalid casts, genuine fights, overload escape, missed bites, cancellation, Keep/Release, duplicate reward prevention and fresh-session collection reload. Existing movement, camera clearance and scene-transition regression checks passed at 07:36 UTC. The separate Input System demonstration also completed a live catch.

Final visual inspection found and corrected opposing fin normals sharing vertices, which produced extreme bloom in the catch reveal. Front/back fin faces now have independent vertices. Disabled fishing presentation also hides the cast guide. Unity-generated YAML retains its customary empty-field whitespace; this is the source of `git diff --check` whitespace notices.
