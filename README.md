# KCES2-CRC3D3-Mods

BepInEx 5 plugins for KCES2 and CRC3D3.

## Supported games

- CRC3D3 second trial (try2), with its 2026.10.22 update.
- KCES2 trial that comes with the CRC3D3 second trial (folder `kces2_try`).

The plugins likely work on, but do not support the third trial (try3) or the retail release yet.

At time of writing, CRC3D3.KCES2LoaderFix also works on COM3D2.5 (which loads KCES1) because of how trivial it is.

## Plugins for KCES2 and CRC3D3

| Plugin          | Effect                                               |
|-----------------|------------------------------------------------------|
| KCES2.SkipIntro | Skips the logo and the caution screen at start.      |

## Plugins for CRC3D3

| Plugin                | Effect                                                                              |
|-----------------------|-------------------------------------------------------------------------------------|
| CRC3D3.DebugTools     | Shows a debug overlay with the field event points in range and their scripts.       |
| CRC3D3.KCES2LoaderFix | Fixes KCES2 started from CRC3D3 (Edit buttons) loading without its BepInEx plugins. |
| CRC3D3.KeyHints       | Shows key hints at the bottom right for keys that other plugins declare.            |

## Local paths

Plugin projects need the game folders.
Copy `LocalPaths.props.user.example` to `LocalPaths.props.user` and set:

- `KcesRoot`: KCES2 folder (the folder with `CR EditSystem.exe`).
- `Crc3d3Root`: CRC3D3 folder (the folder with `CRC 3D 3.exe`).
- `CopyToGame`: `true` copies each plugin DLL to `BepInEx/plugins/<name>/` after build.
- `ExtraPluginRoots`: more folders, separated by `;`, that also get plugins for both programs.

For CI or automated builds, `-p:KcesRoot=...` and `-p:Crc3d3Root=...` win over this file, and
this file wins over the environment variables`KCES2_ROOT` and `CRC3D3_ROOT`.

## Build

```
dotnet build KCES2-CRC3D3-Mods.slnx -c Release
```
