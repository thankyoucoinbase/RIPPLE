# Networking and services

| Part | Version | Where |
|---|---|---|
| Photon Fusion | 2.1.3 Stable, build 2390 (same git hash as the shipped assemblies) | `Assets/Photon/Fusion` |
| Photon Realtime / client library | 5.1.20 | `Assets/Photon/PhotonRealtime`, `PhotonLibs` |
| Photon Voice | 2.64, Realtime 5 edition, with demos (the game compiled them in) | `Assets/Photon/PhotonVoice` |
| Photon Chat | Realtime 5 edition | `Assets/Photon/PhotonChat` |
| PlayFab | the game's own REST client | `Shallows/PlayFabClient.cs` |
| Meta Platform | com.meta.xr.sdk.platform 207 | package |

## Fusion

`Shallows/NetAvatar.cs` is the game's only NetworkBehaviour. It is in pre-weave source form (`[Networked]` auto
properties, `[Rpc]` methods), and Fusion's weaver generates the rest at compile time. After weaving, the result
matches the shipped build: word count 53, every networked property's offset and size, and every RPC's key and payload.
`Assets/Photon/Fusion/Resources/NetworkProjectConfig.fusion` holds the shipped values (weaves Assembly-CSharp,
Assembly-CSharp-firstpass and PhotonVoice.Fusion). The two networked prefabs keep their shipped GUIDs.

## Offline by default

The original developer's IDs are blank: Fusion/Voice App IDs in
`Assets/Photon/Fusion/Resources/PhotonAppSettings.asset`, `PlayFabClient.TitleId`, `MetaPlatform.AppId`, and
`cloudProjectId`. Never put them back. A test client on their IDs could join rooms with real players.

Editor-only guards (`#if UNITY_EDITOR`, not in player builds) block every connect point:

- Photon Realtime: `ConnectUsingSettings`, `ReconnectToMaster`, `ReconnectAndRejoin`
- `NetworkManager.StartRunner`. Fusion keeps its own Realtime copy inside its DLL, so it is guarded at the game's call.
- `PlayFabClient.Call`
- `MetaPlatform.Begin`

Each blocked call logs `[OfflineGuard] blocked <what>`.

## Going online

1. Put your own Photon Fusion and Voice App IDs in `PhotonAppSettings.asset`, your PlayFab title in
   `PlayFabClient.TitleId`, and your Meta app ID in `MetaPlatform.AppId`.
2. Set the environment variable `RIPPLE_ALLOW_ONLINE=1` before starting Unity.

Nothing has been tested with two players yet.
