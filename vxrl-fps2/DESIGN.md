# VXRL-FPS2 — ROOTBREACH

Design and implementation plan · 8 October 2026

Status: original design, followed by an implemented first playable level. See README.md and Validation/ for the actual shipped scope and measured results. The design below includes future work; its timing and content numbers were initial targets, not measured results.

## Direction

A fast, single-player, Doom-inspired FPS set inside a corrupted cyber-security facility. You are a cyber samurai cutting through malware, reclaiming access keys, and destroying the root infection. Use the existing 3D models in deliberately chunky, atmospheric environments.

The defining loop is **explore → fight → collect supplies and keys → open a new route → find secrets → destroy the sector boss → escape**. Levels have authored encounters and interconnected routes. Clearing every enemy is optional except in explicitly signposted boss encounters.

Prioritize movement, weapon impact, readable enemies, and memorable level layouts. First release target: desktop keyboard/mouse, offline, one polished 12–15 minute level. Windows and macOS builds are intended; validate each on its own hardware before claiming support.

## Combat and controls

- WASD movement, mouse look, left-click attack, E interact, 1–5 / wheel weapon selection, Tab automap, Escape pause. Rebindable controls in the finished slice.
- Fast movement by default: start at 9 m/s, quick acceleration, normalize diagonal movement, responsive strafing. Tune against corridor widths and enemy projectiles.
- No sprint stamina, aiming down sights, manual reload, jumping, or crouching in the first slice. Ramps, stairs, lifts, and ledges provide vertical variety without platforming requirements.
- Modern mouse look by default; optional horizontal-only aiming with modest vertical assistance. Assistance must respect walls and line of sight.
- Health starts at 100, with no passive regeneration. Armor absorbs part of incoming damage; health and armor come from placed pickups. Separate ammo pools encourage weapon switching.
- Most incoming ranged damage is visibly dodgeable. Attacks have distinct windups, sounds, and silhouettes. Add brief hit reactions and strong death feedback; standard enemies do not need floating health bars.
- Weapon bob, screen shake, flashes, FOV, and retro pixelation are adjustable. Keep the HUD crisp at all render scales.
- Checkpoint at level start and before the boss. Restart restores a consistent snapshot of inventory, pickups, doors, enemies, and secrets. Add quicksave only after snapshot correctness is proven.

## Weapons

One shared arsenal; Ninja/Shogun artwork selects portrait and identity without splitting the first campaign into separate classes.

| Weapon | Existing visual | Gameplay role | Availability |
|---|---|---|---|
| Katana | `Art/Weapons/Katana/Katana_FPS.fbx` | Fast, short-range fallback; unlimited use | Start |
| Shuriken caster | `Art/Weapons/Shuriken/Shuriken.fbx` | Rapid, accurate projectiles; plentiful ammo | Start |
| Breach shotgun | `Art/Guns/Shotgun/Pfb_shotgun.prefab` | Heavy close-range burst; reliable stagger | Early pickup |
| Yari launcher | `Art/Weapons/YariSpearLauncher/Yari_Spear_Launcher.fbx` | Slow explosive spear; splash and self-damage | Mid-level pickup |
| Purge rifle | `Art/Guns/AssaultRifle/Pfb_assaultRifle.prefab` | Sustained fire with scarce energy ammo | Later expansion |

First slice ships the first four weapons. Tanto can become an alternate melee visual later. Use lower-center/right viewmodels with a large readable silhouette and short recoil animations. Audit existing viewmodel transforms, scale, and material dependencies before adapting them; the old arm/IK rig is not a prerequisite for combat.

## Enemies

Reuse all five existing model families, but give each a distinct combat purpose.

| Existing model | Proposed behavior | Player response |
|---|---|---|
| Virus Scout | Fragile ranged attacker; single slow bolt with clear windup | Strafe and eliminate quickly |
| Malware Brawler | Durable melee pursuer; short committed charge | Bait the charge, punish recovery |
| Worm Burrower | Low-profile ambusher; telegraphed emergence at authored points | Watch the floor and keep moving |
| Ransomware Warden | First-level boss; alternating projectile fan and charge | Use pillars, then exploit recovery |
| Rootkit Overlord | Later episode boss; patterned volleys and limited summons | Change routes and control adds |

First slice uses Scout, Brawler, Worm, and Warden. Avoid spawning enemies directly behind the player without an audible or visible warning. Boss phases must remain solvable with the supplies available in the arena.

## First level: Quarantine Gate

A compact, looping facility with approximately 10–12 spaces, 35–45 regular enemies, one boss, and three secrets. Counts are a playtest budget, not a requirement to overcrowd rooms.

```text
Entry / tutorial ── Security hub ── Blue gate ── Reactor ring
                         │                          │
                    Maintenance                Pump room
                         │                          │
                 Foundry / blue key          Archive / red key
                         │                          │
                         └── hub shortcut       return lift
                                                    │
Security hub ── Red gate ── Warden arena ── Exit / results
```

1. **Entry:** teach movement, shuriken fire, and interaction with two isolated Scouts. Frame the locked exit route through a window.
2. **Security hub:** establish a recognizable central landmark and both color-and-symbol-coded gates. The automap records discovered paths.
3. **Maintenance / foundry:** introduce the shotgun and Brawler. Taking the blue key triggers a bounded ambush and opens a shortcut to the hub.
4. **Reactor ring:** wider circular combat space with cover, visible projectile lanes, and damaging coolant. Introduce the Yari launcher.
5. **Pump room / archive:** Worm ambushes, a short alternate route, red key, and return lift. Reveal the boss arena through glass before entry.
6. **Warden arena:** checkpoint and supply cache before commitment. Several durable cover pillars and a readable escape lane. Defeating Warden enables the exit switch.
7. **Results:** time, kills, items, secrets, restart, and menu. Completion does not require finding secrets.

Secrets: cracked maintenance panel containing armor; a visible cache reached by a remote switch; a reactor side room with extra launcher ammo. Each has a visual/audio clue. Critical keys never depend on secrets, combat drops, or one-way jumps. Gates use symbols as well as color.

## Art and audio

Reuse `Assets/Art/Environment` tileables, block kits, columns/platforms, windows, spawn pads, and collectible meshes. Build new authored layouts rather than reskinning the old arena.

Palette: dark steel and dirty concrete, amber industrial lighting, cyan access systems, toxic green infection, red emergency zones. Use pools of light and contrasting enemy silhouettes; darkness must not conceal necessary routes. Favor large shapes, restrained bloom, minimal motion blur, and sharp texture presentation. An optional reduced-resolution world render supplies the retro feel while retaining 3D enemies and a full-resolution HUD.

HUD: compact bottom bar with health, armor, ammo, selected weapon, portrait, and keys. Reserve central space for aiming. Automap shows explored geometry, gates, and discovered objectives rather than revealing every enemy.

Existing `Assets/Audio/Clips` includes player, weapon, impact, and UI sounds. Audit and reuse suitable clips; add differentiated enemy tells, door/key sounds, and an original or appropriately licensed looping combat track. No dependency on copyrighted Doom artwork or audio.

New visual work is limited initially to keycards, pickup variants, gate symbols, switch states, and a few infection decals. Recolor and assemble existing assets before commissioning additional models.

## Technical approach

Create a standalone Unity project in this folder using the source project's pinned editor, **6000.6.4f1**, and its URP baseline. Confirm local editor/package availability during bootstrap. Carry only necessary packages and lock their resolved versions after the first successful import.

Use GameObjects/MonoBehaviours for single-player runtime, Unity CharacterController for movement, Input System for controls, and NavMesh navigation for ground enemies. These are design choices; exact APIs and package compatibility must be checked during implementation.

The inspected source uses Netcode for Entities. `VirusEnemy` inherits `GhostMonoBehaviour`, uses server/client updates, and explicitly implements direct movement without a NavMesh. `WeaponData` contains ghost-prefab references. Reusing these unchanged would retain multiplayer dependencies and would not provide reliable corridor navigation.

Therefore copy art and dependencies, build new gameplay prefabs, and adapt only self-contained presentation code after inspection. Keep the original project intact. Preserve `.meta` files for copied assets and recursively include referenced materials, textures, shaders, and clips; validate missing references in Unity. Do not copy Library, Temp, Logs, multiplayer bootstraps, or entire gameplay scenes as a starting point.

Proposed systems:

- `PlayerMotor`, `PlayerLook`, `PlayerHealth`: movement, aiming, damage, armor, death.
- `WeaponController` and `WeaponDefinition`: switching, cadence, ammo, hitscan/projectiles/melee, viewmodel feedback. Plain asset references without network ghost types.
- `Damageable`, `Projectile`, `Explosion`: consistent damage attribution, collision filtering, obstruction checks, and pooled effects.
- `EnemyBrain` and `EnemyDefinition`: dormant/patrol, alert, pursue, windup, attack, recover, stagger, dead. Line-of-sight checks, navigation, and deliberately limited perception.
- `Door`, `KeyInventory`, `Switch`, `Lift`, `Pickup`, `SecretZone`, `ExitTrigger`: map interaction with stable IDs for save state.
- `EncounterDirector`: authored triggers and bounded reinforcements; no infinite wave loop.
- `RunState`, `CheckpointStore`, `LevelStats`: versioned local state and deterministic restoration.
- `HUD`, `Automap`, `PauseMenu`, `ResultsScreen`, `AudioController`: presentation and settings.

Bake navigation per level, make locked doors block navigation until opened, and explicitly test lifts and door thresholds. Use modest simultaneous enemy counts, pooled projectiles/VFX, shared materials, and restrained dynamic lighting. Set a 60 fps target at 1080p on a named test machine during bootstrap; treat it as unverified until profiling a standalone build.

Suggested project organization: `Assets/VXRL2/{Scenes,Scripts,Prefabs,Data,UI,Audio,Art}` with imported source assets grouped separately for traceability. Scenes: Boot, MainMenu, CombatGym, E1M1_QuarantineGate.

## Build milestones and completion gates

| Stage | Deliverable | Completion gate |
|---|---|---|
| 1. Bootstrap and asset audit | New project, render settings, imported assets, dependency inventory | Editor opens without compile errors; chosen models/materials render correctly; original project remains unchanged |
| 2. Combat gym | Movement, four weapons, damage, pickups, Scout and Brawler | Movement and shots behave consistently at 30/60/120 fps; no shots through walls; convincing hit/death feedback; death/restart works |
| 3. Playable level blockout | Full route, keys, gates, shortcuts, secrets, navigation | Start-to-exit run without editor intervention; no softlocks; enemies navigate corridors and respect doors |
| 4. Encounter and boss pass | Worm, Warden, final placement and supplies | All encounter types are readable; boss can be beaten without secret supplies; death/checkpoint restoration is consistent |
| 5. Art, audio, and interface | Lighting, materials, HUD, automap, menu, settings, results | Routes remain readable; UI works at 16:9 and 16:10; input/pause/audio settings behave correctly |
| 6. Release validation | Standalone desktop build and short capture | Complete a fresh full run; profile worst combat room; no missing assets/errors; verify checkpoint, restart, and menu transitions |

Build the combat gym before decorating the campaign. Expand to more levels only once the first level passes its completion gates. Suggested later episode: Quarantine Gate → Malware Foundry → Root Core, ending with Overlord.

## Validation and risks

- Asset existence was checked on disk; visual quality, rig behavior, and dependency completeness still require Unity inspection.
- Existing weapon positioning work can inform the new viewmodels, but does not establish compatibility with the new player controller.
- Enemy animation coverage is not yet audited. Start with readable code-driven anticipation/recoil if authored clips are absent; never let animation hide attack timing.
- Prioritize automated checks for key/door state, ammo accounting, damage/armor rules, and checkpoint restoration. Use playtests for movement feel, encounter readability, navigation, and level pacing.
- Explicitly test: full ammo pickup, empty weapon switching, simultaneous damage/death, projectile-wall collisions, locked doors, returning through shortcuts, repeated checkpoint loads, and pause during a boss attack.
- First-slice exclusions: multiplayer, class progression, procedural maps, crafting, inventory grids, extensive cinematics, and a multi-level campaign. These would dilute the combat and exploration milestone.

Next implementation action: initialize the project and import one complete environment module, one enemy, and one weapon; prove rendering and the combat-gym loop before migrating the full asset set.
