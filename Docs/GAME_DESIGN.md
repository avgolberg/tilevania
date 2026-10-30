# TileVania — Treasure Hunt: Game Design (as implemented)

A short 2D pixel-art action-platformer. A young adventurer crosses a forest, descends into ancient ruins, storms an abandoned castle, and defeats the dragon that guards its treasure.

**Flow:** Main Menu → Level 1 (Forest) → Level 2 (Castle) → Treasure Chamber (Boss) → Victory screen → Main Menu.
There is no looping between levels. Progression uses explicit scene names, not build-index arithmetic.

---

## 1. Controls (keyboard first, gamepad bindings included)

| Action | Keyboard | Gamepad |
|---|---|---|
| Move | A / D or ← / → | Left stick / D-pad |
| Climb ladder | W / S or ↑ / ↓ | Stick up/down |
| Jump | Space (or K) | South button |
| Shoot | J, X, Left Ctrl, or Left Mouse | West button |
| Shoot upward | Hold Up + Shoot | Up + West |
| Drop through a wooden ledge | Down + Jump | Down + South |
| Pause | Esc (or P) | Start |
| Menus | Arrows/WASD + Enter/Space, or mouse | Stick + South |

Input is defined in code (`GameInput.cs`) on top of the new Input System, so no scene wiring is needed. UI uses `InputSystemUIInputModule` with its default actions.

---

## 2. Player

| Mechanic | Implementation / tuning |
|---|---|
| Run | Acceleration-based: max 7.5 u/s, ground accel 90, decel 100, air accel 55 |
| Jump | 16.5 u/s launch, gravity scale 4 → apex ≈ **3.5 tiles**. 0.1 s coyote time, 0.12 s jump buffer, variable height (release early to cut), 1.6× fall gravity, max fall 22 u/s |
| Ladders | Press Up/Down on a ladder to grab. The player snaps to the ladder's centre, gravity is off, and climb speed is 4.5 u/s. Jump to leap off; walk sideways to let go. Ladders pass through wooden ledges (`I` tiles). |
| One-way ledges | Jump up through them; Down + Jump drops through. |
| Shooting | A sword-energy slash projectile (16 u/s, ≈14-tile range, 0.3 s cooldown). It spawns in front of the hero at waist height (or above the head with Up) and never inside a wall: point-blank against a wall it shows only a spark. |
| Facing | Driven by horizontal input only. The sprite flips on the child `Visual` transform, so colliders are never mirrored. |
| Health | 3 hearts. A hit costs 1 heart, knocks the hero back, and gives 1.2 s of blinking invulnerability. |
| Death | Brief freeze, then a hop and spinning fall with a red fade, then the **"You Fell"** panel: *Retry from Checkpoint* (full hearts) or *Main Menu*. |
| Pits | Falling out of the level costs 1 heart and fades you back to the last checkpoint. |
| Feedback | Jump/land dust, squash and stretch, landing sound scaled by impact, hit sparks, screen shake, sounds for every important action. |

Animations (Animator states played from code): Idle, Run, Jump, Apex, Fall, Climb, Attack, Hurt.
The hero pack has no climb, hurt or death frames. Climb loops the jump frames, Hurt reuses an apex frame, and death is procedural (see §8).

---

## 3. Enemies and hazards

| # | Requirement | Implementation | Prefab | HP |
|---|---|---|---|---|
| 1 | Stationary, not animated | **Spikes** (contact damage) | `Hazard_Spikes` | — |
| 2 | Stationary, animated | **Slime** (wobbles/hops in place) and **Spider** (dangles on a silk thread, bobbing up and down) | `Enemy_Slime`, `Enemy_Spider` | 2 / 2 |
| 3 | Moving, animated, patrols | **Skeleton**: walks and turns at walls and ledges, detected by raycasts | `Enemy_Skeleton` | 3 |
| 4 | Shoots at the player | **Ghost**: hovers. When the player is within 9 tiles with clear line of sight, it flashes violet for 0.6 s (telegraph), then fires an aimed bolt | `Enemy_Ghost` | 2 |
| + | Flying (optional) | **Bat**: wavy back-and-forth flight | `Enemy_Bat` | 1 |

All enemies take damage through **one hurtbox trigger**, so hits register the same from any direction. Touching any enemy body costs a heart. A defeated enemy bursts into particles with a sound and a small shake. As in the original project, defeated enemies stay defeated when you retry from a checkpoint; reloading or restarting the level brings them back.

---

## 4. Collectibles, keys and chests

| Item | Purpose |
|---|---|
| **Gems** (green/blue/red/orange) | Main collectible, shown in the HUD and on the victory screen. 53 are placed by hand, plus chest rewards (≈86 in total). |
| **Heart** | Restores 1 heart. It stays in the world if you're already at full health. |
| **Gold Key** | Opens the iron **gate**. Level 1: it guards the portal chamber in the ruins. Level 2: it guards the castle's east wing. Keys are per level and consumed on use. |
| **Treasure Key** | Level 2 only. Opens the **Treasure Chamber door** that leads to the boss. |
| **Iron / Silver chest** | Touch to open: an animated lid, then 3 or 6 gems fly out and home in on you, and 1 heart is restored. |
| **Gold chest** | The Guardian's treasure. It appears when the boss dies; opening it triggers victory. |
| **Checkpoint** | A stone pillar that lights a flame (with a 2D light), sets the respawn point and refills hearts. |

HUD (top-left): hearts, gem count, and icons for held keys. The level title card appears at level start, and toasts (keys, locked gates, checkpoints) show at the bottom centre.

---

## 5. Levels

Layouts are ASCII files in `Assets/_Game/Levels/` that the editor builder turns into scenes (see §9).

### Level 1 — The Whispering Forest (`Level1_Forest`, 128×30 tiles)
1. **Meadow (tutorial):** run, jump, gems, a one-way ledge, a slime to shoot, and the first spike dip.
2. **Ruins:** checkpoint, a raised field with a patrolling skeleton, broken columns, a **ladder** up to a ledge holding the **Gold Key**, and a ghost overlooking the area.
3. **Chasm:** a bottomless 4-tile gap.
4. **Underground entrance:** checkpoint, then a ruined gateway with a ladder shaft down into a dark cave. Inside: a skeleton, a spike pair, a hanging spider, an iron chest, a heart on a ledge, and another skeleton.
5. **Portal chamber:** the **Gold Gate** blocks it. Behind the gate are a silver chest and the **exit portal**, which opens with an animation as you approach.
6. Optional: the surface above the cave holds more gems, a ghost, a skeleton and a heart.

### Level 2 — The Abandoned Castle (`Level2_Castle`, 140×34 tiles)
Darker purple/blue palette, torch-lit walls with flickering lights, battlement back-walls, night sky above.
1. **Entrance hall** with wooden ledges.
2. **Spike hall:** wooden bridges over two spike beds, with a bat in between.
3. **Broken bridge:** bottomless gap crossed on three pillars, with two bats and a ghost.
4. **Watchtower:** two ladders and wooden floors up into the night sky, past a spider and a ghost, to the **Gold Key**.
5. **Gold Gate** → **spike gallery:** a 17-tile spike floor crossed on three bridges, with a skeleton patrolling the middle bridge, a bat and a ghost.
6. **Treasure-key tower:** a ladder up to a ledge with a slime, a silver chest and the **Treasure Key**.
7. **Great hall:** two skeletons, a ghost, an iron chest and a heart; then the **Treasure Chamber door** and the portal to the boss.

### Treasure Chamber — Boss (`Level3_TreasureChamber`, 40×22 tiles)
An entry corridor with a checkpoint opens into a torch-lit hall with two side platforms, a central platform and two hearts. Crossing the threshold drops an iron portcullis behind you and wakes the Guardian.

---

## 6. Boss — Guardian of the Treasure

A green dragon (Glax's dragon sprite, scaled 2× with nearest-neighbour filtering) with **30 HP** and a health bar at the top of the screen.

| Attack | Telegraph | Behaviour | How to deal with it |
|---|---|---|---|
| **Fire Volley** | Flies to the wall opposite you, flashes orange and shakes, bell sound | A fan of 3 aimed fireballs (5 when enraged, and twice) | Jump between platforms. It's level with the side platforms, so straight shots hit it. |
| **Dive Slam** | Hovers over you; a **red marker** pulses on the floor for 0.9 s | Slams down, shaking the screen. When enraged, it sends fire shockwaves along the floor both ways. | Move off the marker, then punish it while it recovers on the floor. Jump over the shockwaves. |
| **Fire Rain** | Rises over the hall; **4–6 red floor markers** pulse for 1.1 s | Fire falls on the marked spots. One gap is always left open. | Stand on an unmarked spot and fire upward. |

- **Enraged** below 50% HP: a red tint, a message, shorter pauses, more projectiles and shockwaves.
- Damage feedback: a flash on each hit plus a metallic sound; the health bar drains.
- **Defeat:** chained explosions, flashing, the dragon falls and fades, the music stops, then the **gold chest** appears in a burst of light. Opening it shows **Victory!** with gems, time and deaths.
- **Retry:** if you die, the fight fully resets (HP, position, music) and the door reopens, and you respawn at the corridor checkpoint.

---

## 7. UI and audio

- **Main menu:** a parallax forest diorama with the hero, a treasure chest and a portal. Buttons: Play, Settings (Music/SFX sliders), Credits, Quit. Fully keyboard-navigable.
- **Pause (Esc):** Resume, Restart Level, Music and SFX sliders, Main Menu.
- **Death / Victory panels** as described above; fade-to-black transitions between scenes.
- **Music:** *8-bit Perilous Dungeon* (menu and forest), *Spooky Dungeon* (castle and pre-fight), *8-bit Slay the Evil* (boss fight). Cross-faded.
- **SFX:** Kenney Impact & Interface packs, mapped through `Resources/SfxLibrary.asset` (edit there to swap sounds).
- **Volume:** independent Music and SFX volumes, saved in PlayerPrefs.

---

## 8. Visual design

- Pixel art with point filtering, no compression and no mipmaps. Tiles, hero and items use 16 px = 1 unit. Enemy sheets use 80 PPU to match the hero's size. The dragon uses 8 PPU (an exact 2× upscale).
- Camera: orthographic size 6.75, which is exactly 5× pixel scale at 1080p. It uses smoothed follow with look-ahead, clamps to the level bounds, adds screen shake, and snaps to the pixel grid.
- Forest: natural greens and browns under a dusk sky with stars, clouds and two parallax mountain ranges, background trees and bushes. The underground has darkened rock back-walls.
- Castle: a purple battlement back-wall, blue-grey brick floors, wooden bridges, barred windows, torches with flickering 2D point lights, a dim bluish global light, and a night sky with stars, moon and distant hills in parallax.
- Effects: pooled pixel-particle bursts (dust, sparkles, hits, explosions), squash and stretch, flashes, telegraph pulses.

---

## 9. Technical structure

```
Assets/_Game/
  Art/            imported, organised third-party art (+ Generated/ project-made pieces, Licenses/)
  Audio/          Music/, SFX/
  Levels/         ASCII layouts (source of truth for level geometry & placement)
  Scenes/         MainMenu, Level1_Forest, Level2_Castle, Level3_TreasureChamber (generated, editable)
  Prefabs/        Player, enemies, projectiles, pickups, chests, gates, checkpoint, portal, torch, boss, GameCanvas
  Animations/     Hero clips + Hero.controller
  Tiles/          Tile assets generated from the sliced sheets
  Resources/      SfxLibrary.asset
  Scripts/Runtime TileVania.Runtime.asmdef — Core, Player, Combat, Enemies, Items, Level, UI, Boss
  Scripts/Editor  TileVania.Editor.asmdef — ArtSetup, GeneratedArt, ContentBuilder, UIBuilder, LevelBuilder, GameBuildPipeline
  Tests/PlayMode  TileVania.Tests.PlayMode.asmdef — GameplayTests (19 end-to-end tests)
Assets/_Legacy/   original course project (not in build; contains course-restricted art)
```

Key runtime systems: `GameInput` (input), `Layers` (physics layers and collision matrix, enforced at startup), `GameSession` (gems, deaths, time, keys), `LevelManager` (per-scene flow), `SceneLoader` (fades), `AudioManager`, `CameraController`, `Fx`, `HUD`, `GameMenus`, `DragonBoss`.

**Physics layers:** Ground, OneWay, Ladder, Player, Enemy, Hazards, Interactables, PlayerProjectile, EnemyProjectile. Player projectiles collide only with Ground and Enemy. Enemy projectiles collide only with Ground and Player. Pickups, portals and gates are on Interactables, which only the player touches.

### Rebuilding / editing levels
- **Tools → TileVania → Rebuild Game** regenerates art import settings, prefabs, UI and all four scenes from the layouts. It **overwrites** the generated scenes and prefabs, so make layout changes in the `.txt` files (or stop using the builder and edit the scenes by hand).
- **Tools → TileVania → Validate Scenes** checks for missing scripts and references, empty collider geometry, and the build list.
- **Tools → TileVania → Capture Preview Screenshots** writes `Docs/Screenshots/*.png`.
- **Tools → TileVania → Build Windows Player** builds to `Builds/Windows/`.

**Layout legend** (`y` = 0 is the bottom row; the header line sets the theme and back-wall rule):

| Char | Meaning | Char | Meaning |
|---|---|---|---|
| `#` | solid ground (auto-tiled) | `s` | skeleton (patrol) |
| `=` | one-way wooden ledge | `m` | slime (stationary) |
| `H` | ladder | `o` | ghost (shooter) |
| `I` | ladder + ledge top | `b` | bat (flyer) |
| `^` | spikes | `p` | spider (hangs from the ceiling/ledge above) |
| `P` | player start | `B` | boss start (it flies in from above) |
| `C` | checkpoint | `T` | final treasure spot |
| `g` | gem | `X` | boss-fight trigger |
| `h` | heart | `Z` | arena door (closes behind the player) |
| `k` / `K` | gold key / treasure key | `t` | torch with light |
| `L` / `D` | gold gate / treasure door (2×2, bottom-left anchor) | `w` | castle window |
| `c` / `$` | iron / silver chest | `,` `*` `\|` `A` | grass/bush, flower, column, ruined gateway |
| `E` | exit portal | `.` | empty |
