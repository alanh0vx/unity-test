# vxrl-fps — the original PvE conversion

[![vxrl-fps gameplay](docs/title.png)](https://www.youtube.com/watch?v=YsSH1jBLmko)

▶️ **[Watch the vxrl-fps demo on YouTube](https://www.youtube.com/watch?v=YsSH1jBLmko)**

[`vxrl-fps/`](vxrl-fps/) starts from Unity's **Multiplayer FPS template** (Unity 6, Netcode for Entities) and converts it into a small **PvE** game: play as a **Ninja** or **Shogun** and clear a facility of computer-**virus** enemies with themed weapons (shuriken + short blade, spear-launcher + katana). [ROOTBREACH](vxrl-fps2/) later reused its enemy and weapon art.

## What's in it

- Ninja / Shogun characters with weapon loadouts and 1/2 weapon swap (ranged + melee)
- Virus enemies (5 types incl. 2 bosses) with chase/attack AI, hit feedback, and health bars
- First-person weapon viewmodels, HUD (portrait, ammo, weapon name), minimap, and a victory screen
- A re-themed menu with clickable character cards, plus Single / Multiplayer entry
- Editor tools under **Tools ▸ PvE** to build and register enemies, viewmodels, and placeholder weapons

## How it was built

- **Claude Code** did the PvE conversion inside the template's networked (GhostBridge / ECS) code: the virus enemy and its AI, retargeting hitscan and projectile damage from players to enemies, a new melee weapon type with swing-arc hits, the Ninja/Shogun character select and HUD portraits, weapon swapping, and the editor tools.
- **Codex** modelled the four weapons and five enemies in Blender (see the main [README](README.md)), found and fixed a bug where the weapon swap disabled the whole Shogun player and its camera, and worked on per-weapon hand poses (arm IK). The poses were handed back for manual tuning.

## Status

Work in progress. The core loop (move, swap weapons, fight enemies, win) plays; campaign and lives systems are not built. Built and tested in the Unity Editor (`6000.6.4f1`). Always enter Play mode from the MainMenu scene, not GameScene directly.
