# ASTRA 0.2 — Character system

## Entry points

Open `Assets/OceanariumFishing/Scenes/OF_CharacterCreator.unity` and enter Play Mode, or use the existing Main Menu play button. Confirm writes appearance and travels to Home Island. The existing OF_Player prefab retains its controller, collider and interaction components; its visual child now owns CharacterAppearance instead of CharacterMotion.

## Runtime structure

- `AppearanceData.cs`: version 1 JSON DTO, stable string IDs, catalog, palettes, validation and local atomic save/load. No Unity object references or networking dependencies. Save: `Application.persistentDataPath/appearance-v1.json`, with temporary write and previous-file backup. Missing/invalid IDs revert individually; version 0 accepts the same named fields; future versions safely default without automatically overwriting their file. Confirm is an explicit save.
- `AvatarGeometry.cs`: Unity-native ellipsoid, loft and swept surface mesh generation, normalized bone weights and bindposes. No external models, textures or downloaded assets.
- `CharacterAppearance.cs`: single reconstruction entry point `Apply(AppearanceData)`, shared 18-transform skeleton, color-grouped SkinnedMeshRenderers, one shared URP material and per-renderer property blocks. Owns and destroys generated meshes. Bind-space surfaces follow pelvis/spine/chest/head and paired arm/leg chains. Procedural idle, speed-blended walk/run, turn and interaction gestures share this rig. Skirts blend pelvis and both upper legs.
- `CharacterCreator.cs`: live preview and localized custom GUI, 14 categories, selection cards, palettes, drag rotation, zoom, face framing, reset, randomize and save/continue.

The rig is a humanoid-shaped hierarchy with custom procedural animation, not a downloaded Animator controller. A future Animator/HumanAvatar adapter should take over pose evaluation explicitly rather than running concurrently with the procedural pose driver.

## Content

15 hairstyles; 16 hair/facial-hair/brow colors; 12 skin tones; 6 eye styles and 10 iris colors; 6 brows; 6 mouths; 3 noses; optional freckles/blush/beauty mark/cheek detail; 6 facial hair choices including none. 6 tops, 6 bottoms, 5 shoes, 6 coordinated outfit colors; 5 hats plus none; 3 glasses plus none; backpack, crossbody bag, necklace, bracelet plus none. Presentation is metadata; all choices share the same body and remain available.

Hats tuck away protruding hair geometry. Glasses attach to head, bags to torso/pelvis, bracelet to forearm. One accessory slot is equipped at a time. Outfit colors coordinate top/shoe details; bottoms use the designed navy palette. Long hair is not cloth simulated.

Generated meshes are runtime-owned and grouped by color, named `OF_<color>`; they are not hundreds of duplicated asset files. Existing `OF_Player.prefab` is upgraded in place. Creator preview is a scene object using the same applicator. Assets under `Characters/` contain one avatar surface material and studio materials. The island scene and environment assets are not rebuilt.

## Localization

All user-facing creator text resolves `creator.*` keys through the existing OF_UI Unity Localization tables. English is the source. Turkish creator strings are provided. Other configured locales use English fallback for new creator content until translations are verified; existing island translations remain intact. No claim of 17 newly translated creator dictionaries is made.

## Future integration

Transmit the versioned DTO's stable IDs after server-side entitlement validation. Invoke Apply on the corresponding remote avatar; never call AppearanceStore.Load for a remote player (`loadLocal=false`). Associate authenticated SteamID in a future profile service, outside the cosmetics DTO. Movement authority remains in the existing controller. Catalog IDs can later reference unlock/shop metadata without changing saved selections. No Steam authentication, FishNet synchronization, fishing or aquarium gameplay is added here.

## Validation

Use the editor menu `Oceanarium Fishing/Run ASTRA 0.2 Checks` while the creator is playing. It checks every catalog selection, meshes/materials/bone weights, randomized data, reset, malformed/future-version saves, disk persistence, 17-language fallback and matching Home Island reconstruction. It saves a demonstration character and ends on Home Island. Existing ASTRA 0.1 checks cover input, camera clearance, interactions and travel regressions. Results are written to the ASTRA_02 and ASTRA_01 test reports in this directory.

Visual combination coverage is sampled rather than exhaustive. Standalone executable restart, Steam/network behavior and Animator retargeting require separate validation; editor play-session restart and disk reload are distinct from an executable restart.

## Final observed validation
2026-10-17: Latest creator checks PASS; existing gameplay regression checks PASS. Computer Use verified hairstyle selection, eye style changes with face framing, outfit switching and drag rotation. A fresh Play Mode session restored the saved appearance. Final sleeve correction omits covered skin to prevent shoulder poke-through. No standalone Windows executable restart test was run. Visual quality remains a procedural stylized first pass, not a claim of final reference-art parity.
