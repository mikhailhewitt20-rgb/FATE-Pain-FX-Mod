# FATE Pain FX

Selectable screen overlays that briefly appear when your character takes damage in FATE: Reawakened. The included art is organized into 14 design families. Related pairs evolve to their stronger variant below 35% health, while other designs stay fixed. You can turn evolution off, and adjust opacity and fade time.

## Install and configure

Install through **2rmodman using Thunderstore**. FATE Mod Menu is optional; when installed, Pain FX adds a settings page with the family selector, live preview, enable switch, evolution toggle, opacity, and fade time. The same values are also available in the BepInEx configuration file.

The PNG assets are installed beside the plugin under `BepInEx/plugins/FatePainFX/assets` and are loaded when selected.

## Build from source

Build `src/FatePainFx/FatePainFx.csproj` with the .NET SDK. The project references the game's managed assemblies and BepInEx 5 from a local installation; adjust `GameManaged` and `LoaderCore` in the project file for your setup.

This 0.1.0 build is an unreleased work in progress; there is not an installable package yet.
