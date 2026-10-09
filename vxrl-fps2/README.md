# VXRL-FPS2 — ROOTBREACH

A separate, offline Unity FPS built around fast movement, two distinct character loadouts, keyed exploration, secret caches, and a boss exit. It reuses the original vxrl-fps cyber-enemy and weapon art with new single-player gameplay code.

▶️ **Play in the browser: [fps.vxrl.ai](https://fps.vxrl.ai)** (desktop keyboard/mouse, or landscape on a phone with touch controls)

## Play

On this Mac, open **Builds/macOS/VXRL-FPS2.app**, choose **Ninja** or **Shogun**, then **Enter Quarantine**.

In Unity Hub, open this project with **6000.6.4f1**, open `Assets/VXRL2/Scenes/Rootbreach.unity`, and press Play. The scene generates the authored level from the campaign assets when entering Play mode.

| Control | Action |
|---|---|
| WASD | Move / strafe |
| Mouse | Look |
| Left mouse | Attack |
| 1 / 2 | Class ranged weapon / class blade |
| 3–5 / mouse wheel | Shared pickups / cycle owned weapons |
| E | Open gates, examine loose panels, activate exit |
| Tab | Automap |
| Escape | Pause / resume |

Find the shotgun in Maintenance, blue key in the Foundry, class-appropriate ammunition in the Reactor, and red key in the Archive. Return to the central north passage, open the red gate, destroy Warden, and use the purge terminal. No manual reloads or health regeneration. Armor, health, and ammo are placed throughout the map.

Three difficulty settings adjust incoming damage. Settings include mouse sensitivity, FOV, volume, camera/weapon bob, and minimap visibility. The motion scanner shows explored space, access gates, your heading, and nearby enemy contacts; Tab opens the full map. The game pauses when focus is lost. Checkpoints are saved at the beginning and the Warden approach, with Continue available from the title screen.

## Characters

| Character | Key 1 | Key 2 | Starting armor | Movement |
|---|---|---|---|---|
| Ninja | Shuriken caster | Tanto | 15 | Fast, 9.8 m/s |
| Shogun | Yari launcher | Katana | 45 | Heavy, 8.6 m/s |

Both can collect shared shotgun/rifle pickups. Class-specific ammo and weapon caches adapt to the selected character. A checkpoint retains the selected character and inventory. Checkpoints from builds before character selection are incompatible; start a new run for this version.

## Included

- One complete authored level, 29 enemies, five weapon types across the two class loadouts, two keyed gates, and three hidden caches.
- Five reusable enemy visual/definition assets and six weapon definitions; Overlord and the Purge Rifle are available for future content.
- NavMesh pursuit, line-of-sight attacks, projectile collision, armor, pickups, automap, pause/death/results menus, and checkpoint restoration.
- Editable Campaign, Character, Level, Enemy, and Weapon ScriptableObjects, plus a map preview and content validator.

See [EXTENDING.md](EXTENDING.md) for new levels, enemies, weapons, and build instructions. [DESIGN.md](DESIGN.md) records the original direction; some longer-term design items are intentionally beyond this first playable level.

## Validation

Validation outputs and screenshots are in `Validation/`. The functional tour uses ordinary movement and firing with explicitly counted health/ammo assistance; it is not a claim of unassisted balance testing. The standalone macOS build is the tested platform. No Windows build has been validated.

The initial feature set does not include jumping, crouching, multiplayer, full control rebinding, arbitrary quicksaves, or a multi-level episode. New content using the supplied behavior types is configurable in assets; entirely new attack mechanics require code.

### Mobile browser controls
Play in landscape. The left pad moves; drag the right side to look. Tap the aim
area for one shot or hold FIRE for repeated attacks (drag FIRE to aim while firing).
WEAPON cycles owned weapons in the selected class's order. USE opens doors and
activates terminals. MAP toggles the map; PAUSE opens the existing menu.
Movement, aiming and firing support independent simultaneous fingers. Touches
cancel on pause, focus loss or orientation changes. Keyboard/mouse remain available
on desktop. Mobile rendering is capped at 1.5x device pixel ratio.
Sound starts on the first tap: browsers only allow audio to begin inside a user
gesture, so the page template resumes Unity's audio on the first touch, click or key,
and on iOS 16.4+ plays through the silent switch.

The movement pad is a circular rim with a thumb control; FIRE, WEAPON and USE are
round buttons whose touch areas match the circles.

### Web build and hosting
Build through `VXRL2 > Build WebGL` (or `echo webgl > Validation/editor-job.txt` while
the editor is open). Output goes to `Builds/WebGL-Circular`. Build files are named by
content hash, so a new release never mixes with cached files from an old one. Gzip with
decompression fallback means any static host works without custom headers.
The persistent template lives in `Assets/WebGLTemplates/Rootbreach`, so rebuilding
keeps the mobile viewport, landscape prompt and touch-friendly canvas settings.

The live copy at https://fps.vxrl.ai is served from `~/vxrl-fps-webgl` on the EC2 host
by a localhost-only static server behind an nginx reverse proxy. Deploy the complete
folder:

```bash
cd Builds/WebGL-Circular && rsync -avz --delete -e "ssh -i <key.pem>" ./ ubuntu@<host>:/home/ubuntu/vxrl-fps-webgl/
```

To test locally, serve the folder over HTTP (`python3 -m http.server 8000`); opening
`index.html` from disk does not work. Physical iOS/Android multitouch and performance
are not yet verified.
