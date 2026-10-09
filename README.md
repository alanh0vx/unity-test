# VXRL FPS — AI tooling sample projects

[![ROOTBREACH title screen](docs/vxrl-fps2/title.jpg)](https://fps.vxrl.ai)

▶️ **[Play ROOTBREACH in your browser at fps.vxrl.ai](https://fps.vxrl.ai)** — desktop keyboard/mouse, or a phone in landscape with touch controls.

**Sample / experiment projects**, not finished games. They exist to test how far modern AI tooling can take a game-dev task end to end.

## ROOTBREACH (`vxrl-fps2/`) — the main demo

A single-player Unity 6 (URP) FPS: play as a **Ninja** or **Shogun** and purge a quarantined facility of computer-**virus** enemies. Find the keys, uncover hidden caches, and destroy the Warden boss to reach the purge terminal.

| | |
|---|---|
| ![Shogun gameplay with HUD and motion scanner](docs/vxrl-fps2/gameplay.jpg) | ![Katana strike](docs/vxrl-fps2/katana.jpg) |
| Shogun with the Yari launcher, HUD and motion scanner | Close-range katana strike |

![Mobile browser touch controls](docs/vxrl-fps2/mobile.jpg)
*Mobile browser: movement pad on the left, drag to aim, round FIRE / WEAPON / USE buttons.*

- **Two loadouts:** Ninja (fast; shuriken caster + tanto, 15 armor) and Shogun (heavy; yari launcher + katana, 45 armor), plus shared shotgun/rifle pickups.
- **One authored level:** 29 enemies, blue and red key gates, three hidden caches, and a Warden boss.
- **Interface:** HUD, motion-scanner minimap, full sector map, three difficulty levels, settings, and checkpoint save/continue.
- **Runs everywhere:** a macOS build and a WebGL build with mobile touch controls and sound that starts on the first tap.
- **Data-driven content:** levels, enemies and weapons are ScriptableObjects; editor tooling validates the campaign before building.
- **Validated:** an automated smoke suite (83 checks, including multitouch input) runs on the macOS build. Physical-phone testing is still open.

See [vxrl-fps2/README.md](vxrl-fps2/README.md) for controls, building, and hosting, and [vxrl-fps2/EXTENDING.md](vxrl-fps2/EXTENDING.md) to add levels, enemies and weapons.

## vxrl-fps — the original PvE conversion

[![vxrl-fps gameplay](docs/title.png)](https://www.youtube.com/watch?v=YsSH1jBLmko)

▶️ **[Watch the vxrl-fps demo on YouTube](https://www.youtube.com/watch?v=YsSH1jBLmko)**

[`vxrl-fps/`](vxrl-fps/) starts from Unity's **Multiplayer FPS template** (Unity 6, Netcode for Entities) and converts it into a small **PvE** game with the same Ninja/Shogun theme and virus enemies. ROOTBREACH reuses its enemy and weapon art.

- Ninja / Shogun characters with weapon loadouts and 1/2 weapon swap (ranged + melee)
- Virus enemies (5 types incl. 2 bosses) with chase/attack AI, hit feedback, and health bars
- First-person weapon viewmodels, HUD (portrait, ammo, weapon name), minimap, and a victory screen
- A re-themed menu with clickable character cards, plus Single / Multiplayer entry
- Editor tools under **Tools ▸ PvE** to build and register enemies, viewmodels, and placeholder weapons

Status: work in progress — the core loop (move, swap weapons, fight enemies, win) plays, with campaign/lives systems still to come.

## What was tested

- **[Claude Code](https://claude.com/claude-code)** — gameplay/systems code, editor tooling, UI, debugging, builds and deployment.
- **[Codex](https://openai.com/codex/)** — a parallel coding assistant (e.g. the first-person weapon-pose / arm-IK work).
- **[Blender](https://www.blender.org/)** — 3D assets. The weapon and enemy models were generated with **Codex (Astra)** driving Blender scripting, then imported into Unity.

Both projects use Unity `6000.6.4f1`.
