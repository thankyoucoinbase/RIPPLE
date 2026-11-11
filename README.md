# RIPPLE (Shallows)

Unity project of **Ripple** by BonkoGames (Meta Quest, package `com.bonkogames.shallows`, version 0.1.0), rebuilt
from the shipped Android build. Scripts and shaders are rebuilt one-to-one from the binary and checked against it.

| | |
|---|---|
| Unity | Game built with 6000.3.24f1. Open with **6000.3.11f1**; the bundled URP 17.3 matches the shipped one. |
| Platform | Android (Meta Quest), OpenXR with Meta Quest support, Vulkan with pre-transform, URP, Linear colour, DXT5nm normal maps |
| Networking | Photon Fusion 2.1.3 Stable (build 2390) and Photon Voice 2.64 (Realtime 5 edition), exactly as shipped |
| Services | The game's own PlayFab REST client (`Shallows/PlayFabClient.cs`) and the Meta Platform SDK 207 |

## Open it

1. Open this folder with Unity 6000.3.11f1, Android build target.
2. Open `Assets/Shallows/Scenes/Test3_FullRun.unity` (the first scene in the build) and press Play.

It runs **offline**. The developer's service IDs were removed, and every connect point has an editor-only guard
(`[OfflineGuard] blocked ...` in the console). See [Docs/Networking.md](Docs/Networking.md) to go online with your own IDs.

## Folders

| Path | What it is |
|---|---|
| `Assets/Shallows/` | The game's scenes (`Scenes/Test1_ThePool`, `Test2_Levels`, `Test3_FullRun`) |
| `Assets/Scenes/SampleScene.unity` | The URP template scene the build still lists |
| `Assets/Scripts/Assembly-CSharp/` | All game code (`Shallows` namespace), rebuilt from the binary |
| `Assets/Resources/` | Assets the code loads by name, in the developer's own folder names (`Ape`, `Lifeguard`, `Poolrooms`, `RippleCamera`, ...) |
| `Assets/Shader/`, `Assets/Resources/*.shader` | The 17 game shaders, rebuilt from the shipped Vulkan programs |
| `Assets/Photon/` | Fusion 2.1.3, Voice 2.64, Realtime and Chat from Photon's own packages |
| `Assets/Editor/RippleTools/` | Editor-only checks used to verify the project (not part of the game) |
| `Assets/Material`, `Mesh`, `Texture2D`, `Sprite`, ... | Assets AssetRipper exported by type. A build keeps no paths for them, so their original folders are unknown. |

Only scene and `Resources` paths survive in a build. Scene paths come from the build settings. `Resources` folder
names come from the game's own `Resources.Load` strings, because Unity stores those paths lowercased.

## Status

- **Scripts:** every method rebuilt one-to-one from the ARM64 binary and audited. 0 compile errors. See [Docs/Scripts.md](Docs/Scripts.md).
- **Shaders:** all 59 shipped programs produce the same output as ours on the GPU. See [Docs/Shaders.md](Docs/Shaders.md).
- **Networking:** the Fusion weave matches the shipped build (53 words, 15 networked properties, 17 RPCs). See [Docs/Networking.md](Docs/Networking.md).
- **Play mode (offline):** 0 game exceptions. Renders of the lobby, Banana Shop and pool levels match the Meta Store screenshots.
- **Not tested:** two players online.

## Editor tools (`Tools > Ripple`, or batchmode `-executeMethod`)

| Method | What it does |
|---|---|
| `RippleTools.MissingScriptAudit.Run` | Counts missing scripts in every scene, prefab and ScriptableObject |
| `RippleTools.OfflineGuardCheck.Run` | Calls every guarded connect method and confirms it is blocked |
| `RippleTools.PlayCapture.Run` | Plays a scene offline for N seconds, logs every error and renders views from each level checkpoint |
| `RippleTools.FusionWeaveCheck.Run` | Reads the woven `NetAvatar` by reflection and compares it with the shipped weave |
| `RippleTools.AvatarProbe.Run` | Renders the player gorilla textured the way the game does it |
