# ASTRA 0.5 — Aquarium foundation

## Playing

The Oceanarium museum occupies the reserved Home Island garden at x=8,z=3. Enter along the wooden ramp. Approach a glass tank and press E. Select an individually owned specimen, then Place. The same saved fish appears swimming, with its species color, silhouette, seed, length and weight retained. Remove returns it to available storage. I lists stored fish; displayed fish are managed at their tank. J still opens the existing journal.

Three small displays start unlocked: two tropical saltwater tanks and one temperate saltwater tank, capacity three each. Reception unlocks the six-fish medium marine display. Larger tanks have a locked definition only. Species habitat and individual length are checked before placement. Current eight species are marine; freshwater is supported as a distinct incompatible category, with no invented freshwater content.

Visitors walk from the arrival path through the entrance, choose occupied exhibits, observe, react, visit a second exhibit if available, approach reception and donate before leaving. Donations only arise from a registered live visitor at reception after viewing. Collect them with E at the counter. Spend Coins on comfort, banners, coral/flowers or the medium exhibit.

## Architecture and persistence

FishCollection remains the single owner of individual specimens, records, preferences and catch history. Its schema is now v3; the established filename is unchanged. AquariumSave v1 is nested in the same atomic replacement file. Wallet, pending income, upgrade spending and ownership changes therefore commit together. Existing character appearance storage is untouched. Existing v1/v2 data migrates with an empty aquarium; unsupported collection or aquarium versions remain write-blocked.

Placements use existing FishInstance.ownership and tankId. There is no second aquarium fish inventory. Tank configuration records use stable IDs. Transactions retain GUID visit or collection IDs; upgrade IDs are stable and single-purchase. Failed writes leave live state unchanged. IAquariumAuthority defines the future command boundary. World systems are local today: host authentication, transport, FishNet, Steamworks and leaderboards are not implemented.

Popularity is recomputed from canonical display state: species diversity, rarity, quality, diminishing duplicate contributions, tank-definition variety, habitat, decorations and upgrades. The aquarium economy ScriptableObject exposes weights, five rank thresholds, spawn intervals/capacity, day hours, donation factors and four upgrade prices. The Hour input currently defaults to noon and is a hook for a later clock. No offline income is fabricated.

## Generated content

Runtime: AquariumWorld, AquariumSave, AquariumDefinition, AquariumEconomy, AquariumTank, AquariumReception, AquariumFish, AquariumVisitor, AquariumHUD. Integration edits: FishSpecies, FishingData, FishingController, CollectionHUD and FoundationHUD.

Editor: AquariumBuilder, AquariumLocalization, AquariumChecks and existing AstraEditorBridge commands. Assets under Assets/OceanariumFishing/Aquarium include museum/tank prefabs, reusable glass/roof geometry, procedural materials, tank definitions and economy configuration. OF_Aquarium.shader provides transparent glass and moving water surfaces with reflection-probe sampling. No downloaded asset pack, image, model or audio is used.

The museum has a real collidable floor, accessible ramp, doorway, walls, windows, wood framing, pitched roof, reception, benches, planting, fences and lanterns. The roof rendering cuts away when the player is inside. A small number of existing palms/rocks obstructing the footprint or ramp are deactivated, not deleted. Tourist appearances reuse CharacterAppearance and its skeleton with six palette/accessory combinations. Fish use bounded lightweight steering and animated tails, not per-fish NavMesh agents.

The starter tourist path graph follows known clear central aisles and observation points, with separation from tourists/player and timeout recovery to the exit. It is suitable for this fixed museum layout; arbitrary player-built rooms will require navigation rebuilding. Active visitor positions are session-local; committed donations and completed visits persist.

## Localization

Aquarium keys extend OF_UI. English and Turkish are provided; other configured locales use the existing English fallback. This preserves the 17-locale foundation without claiming complete professional translations. Geometry-based signage uses the Oceanarium proper name.

## Validation

See ASTRA_05_TEST_RESULTS.txt, ASTRA_05_FISHING_RESULTS.txt and ASTRA_05_RESTART_RESULTS.txt for executed outcomes. The end-to-end check drives the existing real Input System Cozy catch demo, keeps fish, opens inventory/journal, walks the player up the ramp through the door, opens a tank using E, places an exact owned specimen, observes swimming, waits for actual visitors/donations, collects income and purchases an affordable upgrade. Negative ownership/duplicate/habitat checks and save reload checks are included. A fresh Play Mode session is checked separately; that is not a standalone executable restart.

Executed checks passed on 2026-10-18: end-to-end aquarium loop, separate Play Mode save restoration, terrain/ramp traversal and UI/fish-volume checks, existing fishing data/balance and runtime scene-transition regressions, and 17-locale fallback/selection checks. No ASTRA 0.6 systems are included. Standalone executable restart, network multiplayer and performance certification remain unverified.

## Shopfront art pass

AquariumShopfront builds layered turquoise shingles, seafoam clapboard siding, ivory window trim, limestone base, fish porthole crest, brass-edged sign, striped canvas awning, open entrance doors, flower boxes and aligned boardwalk handrails. Decorative grass and flower triangles intersecting the building and ramp are removed in cloned meshes; original vegetation mesh assets remain intact. The result is saved in Assets/OceanariumFishing/Aquarium/OceanariumMuseum.prefab and the Home Island scene. Latest physical-entry and exhibit interaction checks passed after this art pass.

