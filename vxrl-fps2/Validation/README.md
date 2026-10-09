# Validation record

## Gameplay regression

`smoke-test.txt` contains 66 passing checks on the macOS release build, including:

- Ninja's Shuriken/Tanto and Shogun's Yari/Katana ownership and key slots.
- Class-aware ammo conversion and original portrait aspect ratios.
- All enemy spawns on navigation meshes, connected key/boss routes, and reachable secret caches.
- Damage/armor, ammunition limits, direct projectile hits, and blocked point-blank shots through thin walls.
- Disk checkpoint restoration of class, inventory, enemies, and pickups; death/restart and boss-gated completion.

## Interface

`UI/` contains screenshots of both class selections and HUDs, the persistent minimap and expanded map, plus 1440x900, 1280x720, and 1024x768 layouts. Screenshots were visually inspected; the imported portrait distortion and misplaced heading marker were corrected.

The font license files and upstream attribution are in `Assets/VXRL2/Fonts/`.

## Traversal and performance

`playtest-tour.txt` records the previous complete route before the class/HUD update: 115.9 average fps at 1440x900 on Apple M3 Pro, p95 frame time 9.33 ms. It used one health/ammo assistance event. Do not treat this as an unassisted balance test or a benchmark for other hardware.

`Shogun/playtest-tour.txt` records the subsequent class-specific route: completed in 96 seconds, 119.6 average fps, p95 frame time 9.31 ms at 1440x900, with one assistance event. Tour health/ammo assistance is counted explicitly in each report. The tour uses ordinary movement, navigation, collision, weapon firing, pickups, and door interactions.

Only macOS has been built and executed. No Windows result is claimed.

## Combat + mobile controls update — 2026-10-09
- macOS release smoke suite: 83 PASS, zero failures (`smoke-test.txt`).
- Real Input System TouchState injection checks simultaneous three-finger movement,
  aiming and firing; release/cancel, class weapon cycling, tap fire, touch pause.
- Desktop touch detection regression included: an unused touchscreen device must
  not suppress keyboard/mouse input.
- `Mobile/mobile-controls.png`, `mobile-small.png`, `mobile-menu.png`: rendered
  phone layouts at 896x414 and 667x375, visually checked.
- `Combat/*`: updated Tanto and Katana contact/follow-through captures, visually
  checked; strokes stay above the status HUD. Weapon audio references verified.
- Local WebGL build loaded and rendered its title screen in Chrome.
- Physical iOS/Android browser multitouch/performance testing remains unverified.
- Live fps.vxrl.ai deployment requires the user's hosting path/command; no live
  deployment was performed as part of these local checks.
