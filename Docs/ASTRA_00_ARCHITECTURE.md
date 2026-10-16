# ASTRA 0.0 architecture and integration boundaries

## Ownership

`Assets/OceanariumFishing` is the active foundation. The earlier `_Oceanarium` proof of concept is retained for recovery, but no active foundation scene depends on it. Template assets and unrelated packages are retained. Empty folders reserve explicit responsibilities rather than claiming implemented systems.

`SceneEntry` is the scene composition root. Serialized references connect it to `OF_Services`, the local controller and HUD. Direct HomeIsland play and Boot -> MainMenu -> HomeIsland both initialize the same services. `GameSession` persists session services, never a global player. `SceneCatalog` maps stable destination enums to build scene names. A loading guard serializes transitions; the future network loading barrier belongs here.

`LocalPlayerController` owns local input and movement. CharacterController provides slopes, ground collision and gravity; input accelerates/decelerates. `CharacterMotion` animates separate limb/head slots procedurally (idle/walk/run support, not final animation). Cinemachine 3 ThirdPersonFollow owns damping and obstruction handling. Right mouse orbits; sensitivity is adjustable in the test settings panel.

`InteractionActor` identifies the interacting actor and holds the temporary starter rod flag. `PlayerInteractor` resolves nearby `IInteractable` components with a bounded nonalloc overlap query. `IslandInteractable` contains the milestone-specific placeholders. HUD subscribes to actor feedback; gameplay does not reference the HUD. Rod claim is local and one-time. Cabin, chest and garden explicitly describe future functionality.

## Future FishNet / Steamworks boundary

This milestone contains no network transport or Steam login. Before networking, replace direct local Interact calls with requests containing authenticated connection identity, stable target ID and request sequence. The host must validate proximity, scene membership, current object state and capacity; only the host commits pickup/economy changes. Replicate authoritative results. Never trust an incoming SteamID or claimed inventory.

Each spawned local player gets input/camera/UI ownership; remote avatars get replicated movement and animation only. Stable IDs on the four rods, chest, cabin and garden are authoring IDs, not Steam identities. The serialized local actor ID is a solo placeholder and must be replaced by authenticated session assignment.

Scene transitions currently run locally through GameSession. Later insert host readiness, disconnect timeout, per-client load acknowledgement and safe-spawn barriers before control is released. Save world state separately from per-player records keyed by world slot and verified SteamID64. No save DTO or schema is claimed in ASTRA 0.0; only language preference is persisted.

## Art and rendering

FoundationBuilder creates persistent Unity scenes, prefabs, mesh assets, materials and lighting. The rebuild command refuses to overwrite an existing authored HomeIsland. EnvironmentPolish updates only its generated meshes/materials and adds a named grass batch in the two presentation scenes. These are editor tools, not runtime object-name dependencies.

All geometry is generated inside Unity: polar organic island mesh, coast skirt, walking-path ribbons, curved palm fronds, batched grass, composite timber architecture and temporary humanoid. Terrain and ocean are custom meshes; other geometry combines built-in primitives into authored silhouettes. No external models, environment packs or textures are used.

OF_CozyOcean is a URP forward shader. Vertex-colored shore distance produces shallow/deep tint, moving shore bands, two gentle waves and analytic sun specular. This first pass uses an authored island-distance field instead of screen-space depth foam, refraction or planar reflections. It is tailored to HomeIsland; new coastlines must regenerate their shore mask. Wave/color parameters are material properties for later weather control. The finite 400 m mesh is hidden by fog before its edge is visible from the island. There is no swimming or underwater simulation.

The dedicated PC pipeline copies the existing PC asset before enabling 4x MSAA and depth texture. Linear color and SRP Batcher are enabled. Materials use instancing where appropriate; a single combined grass mesh reduces renderer count. Warm sun, soft shadows, trilight ambient, neutral tonemapping, slight grading and a 128 px on-awake sky reflection probe provide the first lighting pass. Performance is not yet benchmarked on target hardware.

## Localization and fonts

Unity Localization owns `OF_UI` String Tables and an empty `OF_Assets` Asset Table collection for all 17 locales. Every current player-facing label resolves through a table key. English is the source and default; all untranslated table entries intentionally contain English. The Turkish settings title is a small QA marker, not a claim of translation coverage. LanguageService caches resolved text, supports runtime changes and persists the selected code in PlayerPrefs.

The current lightweight IMGUI test interface uses Unity's default Unicode-capable font for English fallback text. Locale names are displayed in English to avoid implying CJK glyph coverage. Before translated UI ships, replace the test HUD with UGUI/TMP, supply appropriately licensed Latin/Cyrillic plus CJK font assets, connect locale-specific font assets through OF_Assets, configure TMP fallback chains, and run glyph/overflow sweeps with actual translated text. Full CJK glyph validation and font distribution are explicitly unfinished.

## Conventions

Namespace: `OceanariumFishing`, editor namespace `OceanariumFishing.Editor`. Scene/prefab/material assets use `OF_` prefix. PascalCase types and public members, camelCase local/serialized fields. References are serialized or provided at composition time. Runtime and editor assemblies are separate. `.meta` files travel with assets in Git. Generated assets are checked in; Library/Temp/Logs stay ignored.

## Packages

Added direct dependencies: Cinemachine 3.1.7 and Localization 1.5.13, the released Unity 6000.3 versions documented by Unity. Addressables and other required transitive dependencies are recorded in packages-lock.json. Existing URP 17.3.0 and Input System 1.19.0 remain unchanged. UGUI 2.0.0 already provides the TMP package code; no redundant TMP package was added.

Official compatibility references: https://docs.unity3d.com/6000.3/Documentation/Manual/com.unity.cinemachine.html and https://docs.unity3d.com/6000.3/Documentation/Manual/com.unity.localization.html.
