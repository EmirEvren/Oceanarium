# ASTRA 0.1 — Home Island visual delivery

The existing Unity 6000.3.13f1 URP project and ASTRA 0.0 gameplay architecture are retained. All new meshes, texture pixels, materials and scene composition are authored by Unity editor tools. No models, asset packs or packages were downloaded.

## Art direction

The user supplied three tropical harbour references during the work. The daytime Home Island interprets their terracotta roofs, warm timber, broad tropical foliage, pale beach, turquoise water, planted limestone ledges, mooring ropes, lanterns and small explorer with cap and backpack. The city and night images guide the palette and shapes; no town gameplay, night cycle or weather system has been introduced.

## Changes

- Deterministic sculpted terrain with northern hills, irregular shore, vertex-blended beach/meadow/trails, and entry stepping stones. Navigation clearances remain around paths and interactions.
- Curved thicker palms with two crown layers, broad notched leaves, coconuts, rounded tropical/coastal tree variants, shader wind, clustered grass, flowers, shrubs, ferns and rock shelves.
- Existing cabin gained beveled components, layered terracotta shingles, front gable, round attic window, side window, shutters, veranda framing/rails, planters, stool, barrel, crate, nets and lanterns.
- Existing pier gained a broad end platform, four separate holders, thick mooring posts, wrapped and sagging ropes, net, buoys, equipment and lantern. The same four interactable rod identities remain; their positions now match the four holders. Picking up rods is still the only rod gameplay.
- Ocean retains its URP surface and receives smooth waves, view-dependent sky reflection, restrained sun highlights, depth-contact foam and subtle shallow-water interference highlights. Opaque rendering avoids stacked water transparency.
- Daytime lighting uses a warm sun, restrained grading and a custom blue sky with procedural clouds. IslandArtSettings is the reusable art/daylight profile.
- Existing avatar slots, controller and interaction actor remain. Added rounded face geometry, cap, backpack and straps, shorts/socks, connected forearms, idle breathing and smoothly blended walk/run motion.
- Existing Cinemachine camera retains mouse orbit and obstacle handling, with adjusted distance/framing, lower pitch, larger collision radius and gradual obstacle release.
- Fixed geometry is combined by material; reusable lit materials support instancing. Meadow vegetation is one opaque mesh with no shadow casting. Source components are kept disabled for repeatable prefab regeneration; colliders and interaction components remain active.

## Editor tools

- `Oceanarium Fishing > Apply Reference Tropical Direction`: apply the supplied visual direction in the existing MainMenu and HomeIsland scenes. Rebuilds the targeted procedural art and returns to HomeIsland.
- `Apply ASTRA 0.1 Art Pass`: rebuild using the current art settings.
- `Optimize ASTRA 0.1 Static Prefabs`: combine fixed geometry while preserving editable source components.
- `Run ASTRA 0.1 Checks`: execute integration/input/collision checks while playing HomeIsland.
- `Validate ASTRA 0.1 Assets`: validate shader support/compiler messages and finite generated mesh vertices.
- `Measure ASTRA 0.1 Performance`: collect a stationary editor Play Mode sample at the current Game resolution.
- `Capture ASTRA 0.1 Game View`: save the actual rendered Game view.

Art settings: `Assets/OceanariumFishing/Settings/OF_IslandArt.asset`. Seed, hill amplitude, density, wind, sun direction/intensity/colour and ambient/fog colour are exposed. Regeneration deliberately replaces the generated art; hand-authored additions should use separate roots.

## Validation and measurements

See `ASTRA_01_TEST_RESULTS.txt`, `ASTRA_01_ASSET_VALIDATION.txt` and `ASTRA_01_PERFORMANCE.txt` for measured results and test hardware. Input tests inject temporary Input System devices into the real running controllers; camera tests inspect collision clearance at cabin, tree, rock and pier locations. These checks do not substitute for subjective feel testing or a standalone hardware matrix.

## Remaining visual limits

This is a procedural stylized art pass, not a pixel-identical reproduction of the supplied rendered concepts. Character articulation is still procedural rather than a skinned production rig. Water has no planar/SSR reflections, true refraction, underwater visibility or simulated caustics; shallow highlights are a surface approximation. Foliage shadow silhouettes do not animate with its small vertex wind. The small island uses mesh combining and density control rather than a full distance-LOD/streaming system. Static details and cliff surfaces remain visibly stylized and can benefit from further bespoke art direction. No multiplayer performance or standalone mid-range GPU certification is claimed.

All 17 locales remain configured and English is the default. FishNet/Steamworks boundaries are unchanged; no networking, fishing mechanics, aquarium economy, full customization, seasons or weather systems were added. Work stops at ASTRA 0.1.

## Final verified state

- Integration: **67/67 checks passed**, including 17 locales, all eight scene registrations, movement/sprint, cabin collision, four rod identities and interaction, scene transitions and camera clearance samples.
- Assets: **all four custom shaders supported with zero compiler errors**; generated mesh vertex coordinates are finite.
- Final 1920×1080 editor sample: **127.3 FPS average**, 7.86 ms mean frame time, **10.51 ms p95**, 600 frames on RTX 3050 Laptop / i7-11800H. These are stationary editor measurements, not a standalone benchmark.
- Geometry statistics are still substantial: approximately 1,765 draw calls and 4.06 million triangles reported by the editor, where Scene and Game views may both render. Further distance LOD and profiling in a standalone player remain worthwhile before wider hardware targets.
- HomeIsland is left open in Unity Play Mode at the starting viewpoint. Changes are saved locally; no commit or remote publication was requested.

## Delivery files

[Exact changed and added file inventory](ASTRA_01_FILE_INVENTORY.md), [initial visual audit](ASTRA_01_VISUAL_AUDIT.md), [integration results](ASTRA_01_TEST_RESULTS.txt), [asset validation](ASTRA_01_ASSET_VALIDATION.txt), and [final performance sample](ASTRA_01_PERFORMANCE.txt).

Actual Unity captures: [starting view](Screenshots/ASTRA_01_HomeIsland.png), [pier toward cabin](Screenshots/ASTRA_01_Pier.png), [cabin detail](Screenshots/ASTRA_01_Cabin.png), [four rod stations](Screenshots/ASTRA_01_FourStations.png), and [Unity editor window](Screenshots/ASTRA_01_Editor.png). These are game/editor screenshots, not generated concept images.
