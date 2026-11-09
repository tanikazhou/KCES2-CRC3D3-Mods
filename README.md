# KCES2-CRC3D3-Mods

BepInEx 5 plugins for KCES2 and CRC3D3.

## Plugins for KCES2 and CRC3D3

| Plugin          | Effect                                               |
|-----------------|------------------------------------------------------|
| KCES2.SkipIntro | Skips the logo and the caution screen at start.      |

## Plugins for CRC3D3

| Plugin                | Effect                                                                              |
|-----------------------|-------------------------------------------------------------------------------------|
| CRC3D3.KCES2LoaderFix | Fixes KCES2 started from CRC3D3 (Edit buttons) loading without its BepInEx plugins. |

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
