# KCES2.Fusebox

KCES2 & CRC3D3 static code helpers for plugins.

This isn't a shared assembly or Nuget package; this is just code you can copy, or clone via e.g. submodules and add to
your compilation context to avoid dependency hell at BepInEx runtime, at the cost of more code in your DLL.

All types internal for this reason.

## Usage

If you want to compile everything, you can add all files by importing the .projitems file:

```xml
<Import Project=".../KCES2.Fusebox/KCES2.Fusebox.projitems" Label="Shared"/>
```

But you still need dependencies in your .csproj if you want to compile:

```xml
<ItemGroup>
    <PackageReference Include="Microsoft.NETFramework.ReferenceAssemblies" Version="1.0.3" PrivateAssets="all"/>
    <PackageReference Include="BepInEx.Core" Version="5.4.21" PrivateAssets="all" ExcludeAssets="runtime"/>
    <PackageReference Include="HarmonyX" Version="2.9.0" PrivateAssets="all" ExcludeAssets="runtime"/>
    <PackageReference Include="UnityEngine.Modules" Version="2022.3.62" PrivateAssets="all" ExcludeAssets="runtime"/>
</ItemGroup>
```
