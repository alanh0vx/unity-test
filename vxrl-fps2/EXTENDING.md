# Adding content to ROOTBREACH

The game is a standalone Unity 6000.6.4f1 / URP project. Source art is copied into this project, so it does not depend on the original vxrl-fps folder at runtime or when importing.

## Content locations

- `Assets/VXRL2/Data/Campaign.asset`: ordered levels and weapon slots.
- `Assets/VXRL2/Data/E1M1_QuarantineGate.asset`: first level layout and placements.
- `Assets/VXRL2/Data/Weapon*.asset`: weapon definitions.
- `Assets/VXRL2/Data/Virus_Scout.asset` and the other named enemies: enemy definitions.
- `Assets/VXRL2/Prefabs`: normalized visual prefabs.
- `Assets/SourceArt`, `Assets/SourceAudio`: imported source content.
- `Assets/VXRL2/Scenes/Rootbreach.unity`: shared campaign bootstrap scene.

## Add a level

1. Duplicate the existing Level Definition, or use **Create → VXRL2 → Level Definition**.
2. Give it a unique ID and display name. Set start, exit, and checkpoint cells.
3. Edit Rooms. A cell is 4 metres; `Cells` specifies the rectangle in X/Z grid coordinates. Overlapping/adjacent floor cells connect automatically. Corridor rooms should have Decorate disabled. The custom Inspector previews the map.
4. Add enemy placements referencing Enemy Definition assets. Add pickups, doors, and pillars. Keep at least one clear route between objectives. Door `Along X` means the gate's thickness is along the X axis; gates span a three-cell corridor. Key 0 is unlocked, 1 requires blue, 2 requires red. Enable Secret for a disguised loose-panel door. Signs and the four objective text fields are editable per level.
5. If Requires Boss is enabled, include a Boss-role enemy. Put the checkpoint before the boss gate and supplies nearby.
6. Add the new asset to Campaign's Levels list. It becomes selectable from the title screen and reachable through Next Sector after the previous level.
7. Run **VXRL2 → Validate Campaign**, then play the entire route. Verify spawn positions, door clearance, supply balance, navigation, death, restart, and the terminal.

The first version uses blue/red key progression. A campaign with different objective rules needs an extension to the objective/key system; it should not be disguised as a data-only change.

## Add an enemy

1. Import its model and materials. Wrap the visual in a prefab with origin at its feet, positive Z facing forward, and approximately metre-based scale. Gameplay adds the collider and navigation agent.
2. Create an Enemy Definition and assign that visual prefab.
3. Set health, speed, attack damage/interval, notice range, projectile speed, height, color, and role.
4. Existing roles: Ranged fires a telegraphed bolt; Charger pursues and strikes in melee; Ambusher is a fast low-profile melee enemy; Boss fires a spread of projectiles.
5. Place the definition in any level's Enemies list. No central enemy registry change is needed.

Entirely new attack mechanics need a new role and corresponding behavior in `Enemy.cs`. The initial Charger does not yet implement a separate dash, and Ambusher does not yet burrow: the role names describe their current tactical use, not a complete animation system.

## Character loadouts and portraits

`Ninja.asset` and `Shogun.asset` define their portrait, movement speed, armor, starting weapon grants, and inventory order. The first grant is the primary ranged weapon. Portraits use their original pixel aspect ratio (`NPOT Scale: None`) and scale to fit their UI frame. Do not restore power-of-two resizing, which distorts the original images.

Pickup weapon indexes still refer to the global Campaign weapon list. If an ammo or weapon pickup is not in the selected character's allowed inventory, it supplies that character's primary ranged weapon instead. The grant quantity scales using each weapon's pickup-ammo value.

## Add a weapon

1. Create a visual prefab centered at its origin and scaled for a first-person camera; +Z is forward.
2. Create a Weapon Definition. Set damage, interval, ammo limits, sound, model, view transform, and fire mode.
3. Supported modes: Melee, Bolt, Scatter, Explosive, Automatic. Configure pellet count/spread for Scatter and speed/radius for projectiles.
4. Append it to Campaign's Weapons list, then add it to the appropriate Character Definition's Inventory Order. Mouse wheel cycles all owned slots; keys 1–5 select the first five character-specific slots. Keep the class ranged weapon first and blade second.
5. Add a Weapon pickup with its zero-based campaign weapon index. Ammo pickups use the same index. Add a Weapon Grant to the Character Definition's Starting Weapons if it belongs in that character's initial loadout.
6. Test empty ammo, pickup capacity, switching, camera clipping, and wall obstruction.

Entirely new firing mechanics need a corresponding FireMode implementation in `Player.cs`. Avoid reordering existing weapon entries in a published campaign, as pickup references use their slot index.

## Build workflow

`ProjectBuilder.Generate` creates the initial campaign. **Generate First Campaign resets the supplied seed assets**, so do not run it over authored content without a backup. Normal `ProjectBuilder.Build` preserves existing campaign assets and runs content validation before building. The local build job runner accepts only generate/build/play/stop commands through `Validation/editor-job.txt`; it has no network endpoint.

Test saves and validation logs are separate from the player's checkpoint. Current saves describe one checkpoint in one level and include difficulty, map discovery, and a content signature. Changing spawn order, placement, weapon slot IDs, or gate layout invalidates an old checkpoint safely. Validate a fresh run after content changes.

## Interface styling and validation

`GameHUD.cs` draws the interface on a resolution-independent canvas. `GameAssets.asset` supplies local display/body fonts. The larger status bar, class selection cards, portrait frames, motion scanner, and expanded map all use the selected Character Definition.

Use `--ui-gallery --qa-output <folder>` on the standalone executable to capture both class menus and HUDs at several resolutions. `--smoke-test` covers class weapon slots, adaptive ammo, original portrait aspect ratios, and checkpoint character restoration alongside the core gameplay tests. `--playtest-tour --qa-shogun` runs the assisted functional traversal as Shogun.

The normal Build method preserves content edits. **Install Character and HUD Upgrade** is the one-time seed migration and restores the supplied Ninja/Shogun defaults; avoid rerunning it over custom class tuning.

### Blade and throwing presentation
WeaponDefinition supports throwingMotion, contactFraction, windupPosition/Rotation,
cutPosition/Rotation, cutSound and wallSound. Melee resolves one collision during
contactFraction of its interval; switching weapons or resetting combat cancels it.
Attacks pause with gameplay and animate independently of the head-bob option.
Tanto and Katana use distinct poses; Shuriken uses a throwing flick without muzzle flash.
Original synthesized Foley lives in Assets/VXRL2/Audio; regenerate with
`python3 Tools/generate_blade_audio.py`. No third-party audio samples are used.
Use the editor CombatFeelSetup.Configure migration to reinstall the default profiles.

### Touch input
`TouchControls` uses Input System EnhancedTouch and assigns each finger a role
until release. It updates before `Player`; movement/look/fire/use feed the same
player controller and weapon code as desktop. Weapon cycling reads InventorySlots
and ownership, so future class weapons need no hardcoded mobile button changes.
Keep menu touches out of combat and call Clear when transitioning game mode.
`TouchControlQA` injects real Input System TouchState events during the smoke test
for concurrent movement, aim/fire, cancellation, weapon cycling, tap fire and pause.
