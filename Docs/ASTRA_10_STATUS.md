# ASTRA 1.0 implementation and verification status

Implementation in progress; this milestone is **not yet verified in Unity or over Steam**.

## Implemented sources

- User-provided Steam App ID: **4241310**. FishNet listen server, SteamNetworkingSockets transport, Steam lobby discovery/invites, four authenticated players including the host.
- Remote identity comes from the Steam transport. Admission checks lobby membership and Steam's asynchronous ticket validation, with duplicate identity rejection and timeouts.
- Each world uses its own GUID directory and atomic, checksummed save envelope containing shared state and identity-indexed player records. Legacy saves are imported by copying; originals remain intact.
- Owned input/camera, server movement, observer appearance/pose, private owner inventory initialization, shared clock snapshots, disconnect saving and reconnect profile lookup.
- Solo and host resume the owner's saved Home/Town island when accessible. Guests join the host's current island; an incompatible saved location falls back to an authored arrival point.
- Main menu world selection, hosting/joining, privacy, invitations and localized status messages. New strings have English and Turkish translations; other locales use English until translated. Existing translations are retained when setup runs again.
- Full fishing, commerce and building action RPCs remain outside this foundation milestone. Guests currently explore and view their loaded records; host authority gates shared transactions.

## Checks actually completed

Direct C# compilation against the installed Unity reference assemblies passed for FishNet release 4.7.3, Steamworks.NET, FishySteamworks and project Runtime/Editor sources. The project Runtime also passes with editor/development compilation symbols removed. These checks do not execute Unity, FishNet IL postprocessing, physics or network packets.

The initially selected FishNet 4.7.4 failed in upstream Yak code. The manifest pins official release 4.7.3; Unity resolved that commit after the user reopened the project. See ASTRA_10_AUDIT.md for exact commits.

## Pending verification

The user saved and reopened Oceanarium. Unity's fresh import exposed an invalid `OSXStandalone` platform identifier in the embedded FishySteamworks assembly definition; it has been corrected to `macOSStandalone`. Native desktop control still fails with a missing-path error, so the user has been asked to refresh this correction. Network setup is queued for the first successful assembly load. No successful Unity compilation is claimed yet.

After Unity reloads:

1. Resolve package/IL postprocessor errors if any, then run `NetworkBuilder.Build` to generate network prefab, settings and locale entries.
2. Run `NetworkChecks.Run` outside Play Mode. Its isolated save fixtures cover round trips, independent profiles/worlds, backup recovery, interrupted saves, duplicate fish rejection, legacy preservation, client write rejection and saved island fallback.
3. Build a Windows development player and run separate loopback processes using the development probe. Verify movement, ownership, four-player capacity, fifth rejection, disconnect/rejoin and persistence. Loopback tests are not Steam authentication tests.
4. Test actual Steam sessions with separate accounts entitled to App ID 4241310, preferably on separate machines: invites, join, duplicate rejection, host exit, reconnect and world recovery.
5. Recheck solo gameplay, UI locale switching and rendered camera output in Unity.

No player build, generated network resources, live multiplayer success or external Steam test is claimed by this status file.
