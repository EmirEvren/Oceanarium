# ASTRA 1.0 audit

Unity 6000.3.13f1, URP 17.3.0. No existing FishNet, Steam wrapper, transport or App ID was configured. The user supplied Steam App ID **4241310** during implementation.

The existing eight-scene catalog, persistent GameSession and WorldTimeService, procedural CharacterAppearance, CharacterController, fishing, collection, aquarium, building, visitor economy, boats, NPC transactions and deterministic calendar/weather are retained.

Persistence before this milestone: `fish-collection-v1.json` has schema 3 and contains both personal collection/town progression and shared aquarium/travel/calendar state. `appearance-v1.json` contains stable cosmetic IDs. A world-slot adapter must reuse those structures, preserve the original files, and commit shared state and all player records as one atomic document. Fish specimen IDs and legacy collection owner IDs must survive import.

Conflicts found: scene-embedded local players; scene services using FindFirstObjectByType to choose a player; all controllers reading local input; local appearance loading on every avatar; a persistent clock surviving world switches; unscoped save-store calls; a main menu with only one play action. These require explicit local ownership and authority boundaries before adding players.

The selected official sources are FishNet release 4.7.3 (73f30cf2425dc808a4f463f0a233d386010810b3), FishySteamworks release 4.1.1 (21e858249249e2c322365fe9fefbe865f290b0d9), and Steamworks.NET 2025.164.1 (c21a8f0e31c56ae8707130967faf491f7dd7c0d8). FishNet 4.7.3's upstream package.json still says 4.7.2; FishySteamworks' still says 4.1.0. Release tags and pinned commit hashes identify the installed source exactly. FishySteamworks has no assembly definition, so its source is embedded with a project assembly definition referencing the two pinned UPM dependencies and a small SetPeerToPeer configuration method. No second Steam wrapper is installed.

The initial 4.7.4 candidate failed direct C# compilation against Unity's own reference assemblies: its Yak client references absent OnLocalServerConnectionState, IterateIncoming, SendToServer and SendToClient methods. The official 4.7.3 source, Steamworks.NET, FishySteamworks and both project assemblies subsequently passed that same source compilation. This is a compile check, not a substitute for Unity's FishNet IL postprocessor or live transport tests.

Primary references:
- https://fish-networking.gitbook.io/docs/tutorials/getting-started/installing-fish-networking
- https://fish-networking.gitbook.io/docs/overview/readme/features/unity-compatibility
- https://github.com/FirstGearGames/FishNet/releases/tag/4.7.3
- https://github.com/FirstGearGames/FishySteamworks/releases/tag/4.1.1
- https://github.com/rlabrecque/Steamworks.NET/releases/tag/2025.164.1

Compatibility remains subject to compilation and transport tests in this exact project; the official documentation lists Unity 6+ as supported.
