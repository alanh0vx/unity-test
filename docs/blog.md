# I made a small Doom-style FPS with Codex, Claude Code, Unity and Blender

![ROOTBREACH title screen](vxrl-fps2/title.jpg)

Over about a week I put together a small first-person shooter called ROOTBREACH. You pick a Ninja or a Shogun and clear a quarantined facility of computer viruses. It runs in the browser, including on phones, at [fps.vxrl.ai](https://fps.vxrl.ai), and the code is on [GitHub](https://github.com/alanh0vx/unity-test).

I'm not a game developer. The point of the project was to see how far I could get by working with two coding agents, OpenAI's Codex and Anthropic's Claude Code, plus Unity and Blender. I didn't write much code by hand. I wrote prompts, looked at screenshots, played the builds and said what was wrong.

This is what that looked like, including the parts that didn't go smoothly.

## Attempt one: bending Unity's multiplayer template

I started with Unity's official Multiplayer FPS template. It's a polished deathmatch sample built on Netcode for Entities, so it already has movement, guns, a HUD and networking.

I wanted the opposite of deathmatch: players against viruses, solo or co-op. I worked through this mostly with Claude Code. Before touching anything, I had it read the codebase and explain how it fits together. That turned out to be the most useful step. The template has its own pattern for writing networked gameplay as regular MonoBehaviours on top of ECS, and once that was clear the changes went much faster.

Over a few sessions Claude Code added an enemy with simple chase-and-attack AI, made bullets and projectiles hit enemies instead of players, added a melee weapon type with a real swing arc, swapped the two character choices for Ninja and Shogun, and added weapon swapping on 1 and 2.

It worked, and I recorded a [short demo](https://www.youtube.com/watch?v=YsSH1jBLmko).

![The first version, built on the multiplayer template](title.png)

But everything in that template is networked, and that made every small change heavier than it needed to be. Adding a single field to the player state can break serialization. For a single-player shooter, it was the wrong foundation.

## The 3D models: Codex driving Blender

I needed a katana, a short blade, a shuriken and some kind of Japanese spear gun, plus enemies. I can't model, so I asked Codex.

Codex doesn't click around in Blender. It writes a Blender Python script, runs Blender in the background, and exports an FBX for Unity plus a rendered preview image. Then it looks at the preview and fixes what's off. My prompts were short, something like "I'm making an FPS with shooting and close combat, make a katana", then "now a shuriken, a spear gun in Japanese style, and a short blade".

![Katana, tantō, shuriken and yari launcher, all generated as Blender scripts](blog/blender-weapons.jpg)

For enemies I gave even less direction: "computer virus, malware, worms, and bosses, you think for me." It came back with five: a spiky Virus Scout, an armoured Malware Brawler, a segmented Worm Burrower, and two bosses, a Ransomware Warden with a glowing padlock and a floating Rootkit Overlord. It kept the body parts separate so they could be animated later.

![The five enemies](blog/blender-enemies.jpg)

These are static, low-detail models with no rigging or animation. For this kind of game that's fine, and getting them in minutes instead of days changed what felt possible.

## A bug that cost hours

After the new weapons went in, the Shogun stopped working. The game showed "No cameras rendering" and got stuck on "Connecting". The Ninja was fine.

We chased symptoms for a while: camera setup, connection state, audio listener warnings. Codex found the real cause by checking the running editor. The code that hides the template's placeholder gun searched for objects with "Shotgun" in the name. The Shogun's player object was called `ArmaturePlayer_Shotgun`, so the code hid the entire player, camera included.

The fix was one line: only match children whose names start with the gun prefab's name. The lesson for me was to ask the agent to look at the live state instead of reasoning from logs.

## Attempt two: start over, Doom-style

With the art done, I asked Codex to make a new, separate project: "a classic Doom-like FPS, you design and plan how to do it." I added that I wanted it smooth and ready to play, and that I'd want to add levels, enemies and weapons later.

Codex wrote a design doc first, then built ROOTBREACH as a single-player Unity project that reuses the art. It has one level with keyed doors, secret caches and a boss at the exit. There are no reloads, enemies fire dodgeable projectiles, and you move fast. Levels, enemies and weapons are ScriptableObjects, so new content is mostly data.

![Shogun with the yari launcher](vxrl-fps2/gameplay.jpg)

What I didn't expect was how much testing it did on its own. It wrote an automated smoke test that runs inside the real build and checks things like keys opening the right gates, ammo limits, checkpoints restoring correctly, and projectiles not passing through thin walls. It also wrote a route test that plays through the level with normal movement and shooting, and it reported frame rate as well (about 120 fps on my M3 Pro). It was careful to say that the route test gave the player some health and ammo help, so it showed the level could be finished, not that it was balanced.

My feedback rounds were very ordinary game feedback:

- Bring back Ninja and Shogun with their original weapons, and make the HUD look more like Doom.
- The portraits are squashed. (Unity was resizing the image to a power of two. Codex fixed the import setting and the HUD layout.)
- Add a minimap.

## Making it sound right

The first build used a rifle sound for the shuriken and a metal clank for blade hits, so the katana felt like hitting a pipe.

Codex didn't go looking for sound packs. It wrote a short Python script that generates the sounds: an airy flutter for the shuriken, a whoosh for each blade, and a short chop for hitting an enemy. It also added a wind-up, cut and recovery to the melee attacks, and timed the damage to land on the visible cut instead of the button press. That timing change made the melee feel better than any of the sound changes.

![Katana strike](vxrl-fps2/katana.jpg)

## Phones

Then I asked for the game to work on a phone browser. It loaded, but you couldn't do anything.

The player controller simply stopped updating when there was no keyboard or mouse. Codex removed that dependency and added a movement pad on the left, drag-to-aim on the right, hold-to-fire, and buttons for weapon, use, map and pause. It also wrote tests that inject several touches at once, to check you can move, aim and fire together and that letting go never leaves you stuck firing.

![Touch controls on a phone-sized screen](vxrl-fps2/mobile.jpg)

Two small things came up during browser testing. Desktop Chrome can report a touchscreen even when you're using a mouse, so detection now waits for a real touch. Safari crashed on an old cached build, and Codex added content-hashed file names so a new release never mixes with old cached files.

## Shipping it, and switching agents halfway

I asked Claude Code to export the game to WebGL and help me host it. It added a build step and gave me an nginx config for my own server, and I set up the subdomain. Uploads after that were a single rsync command.

Midway through, Codex ran out of usage while it was finishing the round touch buttons. I pasted the last few lines of its output into Claude Code and said "pick up where it left off." Claude Code found that the error Codex was looking at was actually a success message, reran the phone-layout screenshots and tests, rebuilt, and deployed. The project files, the build scripts and the test outputs in the repo were enough for a different agent to carry on.

The last bug was sound on phones: there was none. Browsers only let a page start audio during a tap, and Unity tries to start its audio from a timer, so on a phone it never started. The fix was a few lines in the web page that resume audio on the first touch, and on iPhones also let the game play when the silent switch is on.

## What I'd tell someone trying this

- **Ask for a plan and a design doc first.** Both agents did better work once there was a written target.
- **Make the agent look, not guess.** Screenshots of the actual build, logs from the running editor, and automated checks in the real game caught most problems. When an agent reasoned from code alone, it often fixed the wrong thing.
- **Give game feedback, not code feedback.** "The katana sounds like hitting steel and doesn't chop" worked better than describing a fix.
- **Pick the right foundation.** The multiplayer template taught me a lot, but a simple single-player project was easier to change and much easier for the agents to test.
- **Keep the state in the repo.** Design docs, build scripts and test results made it easy to switch between agents, and between sessions.
- **Blender through scripts is good enough for props.** I wouldn't use it for characters with animation yet, but for weapons and blocky enemies it was great.

## What's still rough

- The models aren't animated. Enemies slide and turn rather than walk.
- Physical phone testing has been light. The touch tests run in the editor build, not on real devices.
- There's one level. The data-driven setup should make more levels easy, but I haven't proven that yet.

You can play it at [fps.vxrl.ai](https://fps.vxrl.ai). On a phone, turn it sideways and tap once to get sound.
