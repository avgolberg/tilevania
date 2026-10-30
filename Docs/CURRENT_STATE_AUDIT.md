# TileVania — Current State Audit

**Audit date:** 2026-10-30
**Scope:** Read-only static audit of `D:\Unity Projects\TileVania` (scenes, prefabs, scripts, assets, packages, project settings, Git state).
**Method:** All findings come from reading the project's YAML/C# files directly, plus the current Unity Editor log. **No runtime/play-mode testing was performed.** Where a behavior is inferred from code or data, not observed in play, it is marked:

| Marker | Meaning |
|---|---|
| ✅ **Confirmed (static)** | Directly visible in the files (code, serialized values, settings). |
| ⚠️ **Needs runtime check** | Likely behavior based on code + Unity physics rules, but not observed in play. |
| ❓ **Unknown** | Cannot be determined from the project contents. |

---

## 1. Executive summary

TileVania is a small, compact 2D tile-based platformer built with **Unity 6000.2.13f1 + URP 2D**. It has **2 short single-screen-ish levels** (each a 32×17-tile room), a player who can **run, jump, climb ladders, bounce on a mushroom, and shoot projectiles**, **patrolling enemies**, **spike and water hazards**, **coins with score**, **3 lives**, and a **level-exit portal** that loads the next scene in build order. There is **no menu, pause, game-over screen, victory screen, music, or settings**.

The codebase is tiny (**7 scripts, ~250 lines**) and follows the standard course structure. It compiles cleanly in the open Editor session. It works as a learning prototype, but it relies on per-scene overrides, string lookups, and build-index ordering, and has several logic flaws (listed in §7).

**The most important non-technical finding is licensing.** `Assets/Sprites/README - License details.txt` says the `SPA_*` art (player, enemy, tiles, background, coins, ladder) comes from Foxfin's *Super Platformer Assets* under a GameDev.tv **student/personal license that forbids use in a published game** unless you buy the pack from the Unity Asset Store. The origin of several other assets (font, sound, spikes, mushroom, doorway, projectile) is not documented.

**Top findings at a glance**

1. 🔴 Art licensing blocks publishing in the current state (see §5.7).
2. 🔴 Death reloads the scene immediately, so the death animation and the "death kick" are effectively never seen (§7, I-2).
3. 🟠 Projectiles are on the **Player** layer, not the existing **Bullets** layer. That lets bullets trigger the **Exit Portal** (level skip) and collide with the player (§7, I-3, I-4).
4. 🟠 The Player prefab is `Untagged`, and the Coin prefab's collider is **not** a trigger. Both are fixed only by per-instance scene overrides, so a newly placed Player or Coin breaks coin collection (§7, I-5).
5. 🟠 Finishing the last level silently wraps back to Level 1 with score and lives kept. There is no end state (§7, I-6).
6. 🟡 Only one sound effect exists. There is no music, and the HUD is two unlabeled numbers.
7. 🟢 There are good reusable foundations: Tilemap + Rule Tile setup, layer/sorting-layer scheme, Cinemachine 3 state-driven camera with confiner, Input System actions, and prefab-based level objects.

---

## 2. Project and technical overview

### 2.1 Engine, pipeline, platform settings ✅

| Item | Value | Source |
|---|---|---|
| Unity Editor | **6000.2.13f1** (Unity 6.2) | `ProjectSettings/ProjectVersion.txt` |
| Render pipeline | **URP 17.2.0**, 2D Renderer | `Assets/Settings/UniversalRP.asset`, `Assets/Settings/Renderer2D.asset` |
| Lighting | One `Global Light 2D` (Global type, intensity 1) per scene | `Assets/Prefabs/Global Light 2D.prefab` |
| Post-processing | `Assets/DefaultVolumeProfile.asset` exists. No Volume objects in scenes. | — |
| Color space | Linear (`m_ActiveColorSpace: 1`) | `ProjectSettings/ProjectSettings.asset` |
| Input handling | **Input System only** (`activeInputHandler: 1`) | `ProjectSettings.asset` |
| Product / company | `TileVania` / `DefaultCompany`, version `1.0` | `ProjectSettings.asset` |
| Default resolution | 1920×1080, Fullscreen Window, not resizable | `ProjectSettings.asset` |
| Physics 2D gravity | (0, −9.81), default settings | `ProjectSettings/Physics2DSettings.asset` |
| Pixels per unit | 32 for all `SPA_*` sheets, spikes, mushroom, projectile. **100** for `doorway-white.png` | Sprite `.meta` files |
| Filter mode | Point (0) on all sprites (pixel-art friendly) | Sprite `.meta` files |

**Editor status during audit:** the Unity Editor (PID 15332, 6000.2.13f1) **already had this project open** when the audit ran. It was not opened by this audit. The current `Editor.log` shows **no C# compile errors or warnings** from project scripts. The only notable log line is an internal Cinemachine package notice.

### 2.2 Folder structure ✅

```
Assets/
  Animations/   6 .anim clips, 3 .controller, Cinemachine Blender Settings.asset
  Audio/        Coin-Pickup.ogg
  Fonts/        Symtext.ttf, Symtext SDF.asset (TMP font asset)
  Materials/    Bounce.physicsMaterial2D, Slip.physicsMaterial2D
  Prefabs/      12 prefabs (Player, Enemy, Red Enemy Variant, Bullet, Coin, Big Coin Variant,
                Exit Portal, Game Session, Scene Persist, Main Camera, State-Driven Camera,
                Global Light 2D)
  Scenes/       Level 1.unity, Level 2.unity
  Scripts/      7 C# scripts (flat, no namespaces, no asmdef)
  Settings/     URP assets, 2D scene template
  Sprites/      10 PNGs + Rule Tile.asset + 3 sprite materials + license README
  TextMesh Pro/ TMP Essentials (LiberationSans, EmojiOne, shaders)
  Tiles/        Main Palette.prefab + 50 tile assets
  _Recovery/    3 crash-recovery scenes (git-ignored, see §7)
  InputSystem_Actions.inputactions, DefaultVolumeProfile.asset,
  UniversalRenderPipelineGlobalSettings.asset
```

Organization is clean and conventional for a course project. There are no feature folders, namespaces, or assembly definitions, and no tests.

### 2.3 Packages (`Packages/manifest.json`) ✅

| Package | Version | Used by the game? |
|---|---|---|
| `com.unity.render-pipelines.universal` | 17.2.0 | **Yes** (2D Renderer, Light2D) |
| `com.unity.inputsystem` | 1.14.2 | **Yes** (`PlayerInput`, UI input module) |
| `com.unity.cinemachine` | 3.1.5 | **Yes** (CM3 State-Driven Camera, Position Composer, Confiner2D) |
| `com.unity.2d.tilemap` / `.tilemap.extras` | 1.0.0 / 5.0.1 | **Yes** (Tilemaps, Rule Tile) |
| `com.unity.ugui` (incl. TextMeshPro) | 2.0.0 | **Yes** (HUD) |
| `com.unity.2d.sprite` | 1.0.0 | Yes (sprite slicing) |
| `com.unity.visualscripting` | 1.9.8 | Only via an **unused** `using Unity.VisualScripting;` in `EnemyMovement.cs` |
| `com.singularitygroup.hotreload` | 1.13.15 (**Git URL**: `gitlab.hotreload.net`) | Editor dev tool (third-party) |
| `com.unity.2d.animation`, `.aseprite`, `.psdimporter`, `.spriteshape` | — | No usage found |
| `com.unity.timeline`, `com.unity.test-framework`, `com.unity.multiplayer.center`, `com.unity.collab-proxy` | — | No usage found |
| IDE packages (Rider, Visual Studio) | — | Editor only |

### 2.4 Git state (inspected read-only) ✅

- Remote: `origin → https://github.com/avgolberg/tilevania.git`, branch `main`, up to date with `origin/main`.
- 20 commits, 2026-01-05 → 2026-01-19. Latest commit: `1921e3d Add game session controller and scene persistence`.
- **Working tree is dirty:** `Assets/Fonts/Symtext SDF.asset` is modified (+9/−178 lines). This looks like a TMP dynamic atlas being cleared/regenerated by the Editor. Nothing was changed by this audit.
- Git reports "dubious ownership" for this drive (filesystem without ownership info). The audit worked around it per command (`git -c safe.directory=*`) and **did not change global Git config**.
- `.gitignore` excludes `Library/`, `Logs/`, `UserSettings/`, IDE files, and `Assets/_Recovery/`.

### 2.5 Entry point and expected flow ✅

- **Build scenes:** `0 = Assets/Scenes/Level 1.unity`, `1 = Assets/Scenes/Level 2.unity`. There is no bootstrap, menu, or loading scene.
- **Flow:** the game starts directly in Level 1. Touching the Exit Portal loads the next build index after 1 s. After Level 2 it **wraps to Level 1**. Losing all lives reloads build index 0 and resets the session.

### 2.6 Technical / compatibility concerns

| Concern | Detail |
|---|---|
| Unity 6.2 is not an LTS stream | 6000.2 is a "Supported/Update" release. The long-term lines are Unity 6.0 LTS and 6.3 LTS. Check current Unity support status before committing to a version (§10). |
| Git-sourced third-party package | Hot Reload is resolved from a GitLab URL. A fresh clone needs network access to that host, and its license terms should be reviewed. |
| Unused packages | Add import time and noise. Visual Scripting is pulled in only by a stray `using`. |
| Mixed PPU | `doorway-white.png` uses 100 PPU while everything else uses 32. Scale consistency depends on manual transforms. |
| `_Recovery/` scenes | Three crash-recovery scenes sit inside `Assets/` (git-ignored). Unity imports them, and they appear in the project. |

---

## 3. Scene and level inventory

### 3.1 Scenes ✅

| Scene | In build | Role |
|---|---|---|
| `Assets/Scenes/Level 1.unity` | Index 0 | First level and de-facto entry point |
| `Assets/Scenes/Level 2.unity` | Index 1 | Second (final) level |
| `Assets/Settings/Scenes/URP2DSceneTemplate.unity` | No | URP template, not gameplay |
| `Assets/_Recovery/0.unity`, `0 (1).unity`, `0 (2).unity` | No | Editor crash-recovery copies from Jan 15, 17 and 20 (see §7, I-14) |

### 3.2 Common scene composition (both levels) ✅

Each level contains:

- **`Tilemaps`** (Grid) with child Tilemaps:
  | Tilemap | Layer | Sorting layer | Collider |
  |---|---|---|---|
  | `Background` | Background | Background | PolygonCollider2D (used **only** as the camera confiner shape) |
  | `Platforms` | Ground | Platforms | TilemapCollider2D merged into CompositeCollider2D (Outlines) |
  | `Ladder` | Ladder | Ladder | Composite, **trigger** |
  | `Hazards` (spikes) | Hazards | Hazards | TilemapCollider2D, **trigger** |
  | `Water` | **Hazards** | Water | TilemapCollider2D, **solid** (not trigger) |
  | `Bouncing` (mushroom) | Bouncing | Platforms | TilemapCollider2D with `Bounce.physicsMaterial2D` |
- Prefab instances: `Player`, `Main Camera`, `State-Driven Camera`, `Global Light 2D`, `Game Session`, `Scene Persist` (coins and enemies are **children** of Scene Persist), `Exit Portal`.
- `EventSystem` with `InputSystemUIInputModule`: **Level 1 only**. Level 2 has none.

Both levels use the **same room size**: Background 32×19 tiles (x −20…11, y −11…7). Walls and ceiling are solid platform tiles.

### 3.3 Level 1 layout ✅ (rendered from tile data)

Legend: `#` platform, `H` ladder, `^` spikes, `~` water, `M` mushroom (bounce), `P` player spawn, `X` exit, `c` coin (100), `B` big coin (250). Rows are tile Y coordinates.

```
   5 ################################
   4 ###...........................##
   3 ###........................H..##
   2 ###...B........####........H..##
   1 ###.X..#^^^#####......####.H..##
   0 ##############.............H..##
  -1 #####......................H..##
  -2 ##..................M......H..##
  -3 ##.................###.....H..##
  -4 ##........................cH..##
  -5 ##.P.....c...####........####.##
  -6 ##............................##
  -7 #####...###...................##
  -8 #####~~~###~~~~~~~~~~~~~~~~~~~##
  -9 #####~~~###~~~~~~~~~~~~~~~~~~~##
 -10 ################################
```

- **Route (inferred from layout):** spawn on the small bottom-left ledge → hop across a water gap via a small island and floating platforms → reach the mushroom and/or the right-side ladder (x = 7, 8 tiles tall) → climb up → travel left across the upper area → cross the spike pit on the top shelf → Exit Portal at the top-left, directly above the spawn.
- **Contents:** 2 coins + 1 big coin (max 450 pts), 3 spike tiles, 1 mushroom, 1 ladder, **no enemies**.

### 3.4 Level 2 layout ✅

Legend as above; `E` enemy, `R` red enemy.

```
   5 ################################
   4 ###....#########################
   3 ##.....######.#####..#####.B.###
   2 ##.c........#.........####....##
   1 ##.H...........................#
   0 ##.H....E........###...........#
  -1 ##.H...######c.................#
  -2 ##.H..########..............M..#
  -3 ##.H..######........c.....####.#
  -4 ##.H.^####.........####........#
  -5 ##.H.#####c....#^^^#~~~~~~~~~~~#
  -6 ##.H.#.c###...######~~~~~~~~~~~#
  -7 ##.H^#...........###~~~~~~~~~~~#
  -8 ##PH##.XR...#~~~~###~~~~~~~~~~~#
  -9 #############~~~~###~~~~~~~~~~~#
```

- **Route (inferred):** spawn bottom-left next to a 10-tile ladder (spike right beside it) → climb to the top → explore the open middle and right (water pool bottom-right, spike row, mushroom, big coin in the top-right nook) → drop down a shaft (around x −9…−7) into the bottom pocket → Exit Portal guarded by the Red Enemy. The exit is only about 4 tiles from the spawn, behind a wall, so the level is a loop.
- **Contents:** 5 coins + 1 big coin (max 750 pts), 5 spike tiles, large water area, 1 mushroom, 1 ladder, 2 enemies (`Enemy` speed 1, `Red Enemy` speed overridden to 1.5).
- Level 2's Background tilemap is identical to Level 1's. Level 2 looks like it was built from a copy of Level 1.

### 3.5 Reusable level components ✅

| Component | Path | Notes |
|---|---|---|
| Rule Tile (13 rules) | `Assets/Sprites/Rule Tile.asset` | Auto-tiling for `SPA_Rock_Grass_Water` platforms |
| Tile Palette | `Assets/Tiles/Main Palette.prefab` | 50 tile assets (background 16, rock/grass/water 30, ladder, spikes, mushroom) |
| Level object prefabs | `Assets/Prefabs/*.prefab` | Player, enemies, coins, exit, session, persistence, cameras, light |

### 3.6 Progression and expansion opportunities

- Progression = **build order** (`buildIndex + 1`). Adding a level means adding it to Build Settings in the right order. No level select, saves, or unlock data.
- Expansion opportunities: larger scrolling levels (the camera and confiner already support scrolling), more hazard and enemy placement, vertical sections using the ladder camera zoom, and secrets in background areas. Level 1 has no enemies at all.

---

## 4. Gameplay systems inventory

Status key: **Implemented** = complete code path and data present. **Partial** = present but incomplete or fragile. **Unused** = configured but never used. **Possibly broken** = static analysis suggests incorrect behavior.

### 4.1 Controls (Input System, `PlayerInput` using *Send Messages*) ✅

| Action | Keyboard / Mouse | Gamepad | Used? |
|---|---|---|---|
| Move | WASD / Arrow keys | Left stick | ✅ `OnMove` |
| Jump | Space | South button | ✅ `OnJump` |
| Attack (shoot) | Left mouse, Enter | West button | ✅ `OnAttack` |
| Look, Interact, Crouch, Sprint, Previous, Next | (template defaults) | | ❌ Unused |
| UI map | (template defaults) | | Only the EventSystem in Level 1 |

Asset: `Assets/InputSystem_Actions.inputactions` (Unity's default template, unmodified in structure). Control schemes for Touch, Joystick and XR are present but untested.

### 4.2 Player — `Assets/Scripts/PlayerMovement.cs` on `Assets/Prefabs/Player.prefab`

| Mechanic | Status | Details |
|---|---|---|
| Horizontal run | Implemented ✅ | `moveSpeed = 8`. Velocity is set directly every `Update`, with no acceleration or deceleration. `Slip` material (friction 0) on the body capsule prevents wall sticking. |
| Sprite flip | Implemented ✅ | Flips via `localScale.x = ±1`. |
| Jump | Implemented, with caveats | `jumpSpeed = 25`, gravity scale 8 → apex ≈ **4.0 tiles**, flat-ground jump distance ≈ 5.1 tiles. Grounded check = feet `BoxCollider2D` touching the **Ground** layer. **No** coyote time, jump buffering, variable jump height, or jump/fall animation. Jump *adds* to the current Y velocity (`velocity.y + jumpSpeed`) instead of setting it ⚠️ possible stacked or inconsistent jumps. |
| Ladder climbing | Implemented ✅ | When the feet touch the **Ladder** layer, gravity becomes 0 and vertical speed = `moveInput.y × 5`. Horizontal movement is still allowed. No jump from a ladder (Ladder ≠ Ground). |
| Bounce | Implemented ✅ (data-only) | Mushroom tile on the Bouncing layer with `Bounce` material (bounciness **1.2**, above 1 so it adds energy). The player cannot *jump* off it (not Ground). ⚠️ The feel needs a runtime check. |
| Shooting | Implemented, with issues | Spawns `Bullet.prefab` at child `Gun` (local x 0.26). No cooldown, ammo, or animation. See §4.5 and §7. |
| Death | Partial / possibly broken | Dies if the body touches **Enemy** or the **feet** touch **Hazards** (spikes and water). Sets the `Dying` trigger and death-kick velocity (10, 20), then **immediately** calls `GameSession.ProcessPlayerDeath()`, which reloads the scene in the same frame (§7, I-2). Side contact with spikes does not kill. |
| Animations | Partial | `Player.controller`: Idling (default, 4 frames) ↔ Runnung (10 frames, 1.5× speed) ↔ Climbing (2 frames, 0.5×). Any State → Dying (1 frame). No Run ↔ Climb transition, and no jump, fall, shoot or hurt states. |
| Components | ✅ | Rigidbody2D (dynamic, gravity 8, freeze rotation, continuous collision, **no interpolation**), CapsuleCollider2D (body), BoxCollider2D (feet), Animator, PlayerInput. |

### 4.3 Enemies — `Assets/Scripts/EnemyMovement.cs`

| Item | Status | Details |
|---|---|---|
| `Enemy.prefab` | Implemented | Tag `Enemy`, layer Enemy. **Kinematic** Rigidbody2D. Capsule body + small front **trigger** box (ledge sensor). 6-frame walk animation. `moveSpeed = 1`. |
| `Red Enemy Variant.prefab` | Implemented | Tinted (1, 0.52, 0.52), `moveSpeed = 3`. The Level 2 instance overrides this to 1.5. |
| Patrol | Implemented, fragile ⚠️ | Constant velocity. **Reverses on *any* `OnTriggerExit2D`** (ledge sensor leaving ground, or the body leaving spike, water or player contact). Kinematic bodies are not stopped by walls, so turning at walls depends on trigger-exit events only. Behavior at Level 2's 1-tile bump near the exit (x ≈ −8) needs a runtime check. |
| Damage to player | Implemented ✅ | Contact kill (player body touching the Enemy layer). |
| Enemy death | Partial ⚠️ | Destroyed only when a bullet enters an enemy **trigger** (the front sensor). Bullets hitting the solid capsule only schedule the bullet's own destruction after 1 s. Whether hits from behind kill reliably needs a runtime check. No death animation, sound or score. |

### 4.4 Hazards ✅

| Hazard | Implementation | Notes |
|---|---|---|
| Spikes | `Hazards` tilemap, trigger, Hazards layer | Kill on feet contact only. |
| Water | `Water` tilemap, **solid** collider, Hazards layer | Kill on feet contact. The player technically lands on the water surface for a frame before the reload. |
| Enemies | see 4.3 | |
| Pits / out-of-bounds | None | Rooms are fully enclosed, so no kill-Z is needed in current levels. |

### 4.5 Projectile — `Assets/Scripts/Bullet.cs` on `Assets/Prefabs/Bullet.prefab`

- Sprite `Shooting Star.png` at scale 0.3, speed 20. Direction is taken from `player.localScale.x` (via `FindFirstObjectByType<PlayerMovement>()` at spawn). The sprite is **not flipped** when firing left.
- Non-trigger CircleCollider2D, dynamic body, gravity 0, **layer = Player** (the dedicated `Bullets` layer exists but is unused).
- `OnTriggerEnter2D`: if the other object is tagged `Enemy`, destroy it. Always destroy the bullet.
- `OnCollisionEnter2D`: destroy the bullet after **1 s**. Velocity is re-applied every frame, so it keeps pushing against what it hit.
- Status: **Implemented, possibly broken** in several interactions (§7, I-3, I-4).

### 4.6 Collectibles and scoring — `CoinPickup.cs`, `GameSession.cs` ✅

- `Coin.prefab`: 12-frame spin animation (0.6× speed), +100 points, Interactables layer. `Big Coin Variant.prefab`: 2× scale, gold tint, +250.
- Pickup: `OnTriggerEnter2D`, requires tag `Player` and is guarded by `wasCollected` (the player has two colliders). Calls `GameSession.AddToScore`, plays `Coin-Pickup.ogg` via `AudioSource.PlayClipAtPoint`, then disables and destroys itself.
- **Fragility:** `Coin.prefab`'s collider is **not** a trigger, and `Player.prefab` is `Untagged`. Collection works only because **every scene instance overrides** `m_IsTrigger = 1` (coins) and `m_TagString = Player` (player). `Big Coin Variant` does set the trigger in the variant itself.
- Coins also carry an unused `AudioSource` component (no clip, Play On Awake).
- Score is used only for display. There is no high score, no save, and no reward for coins.

### 4.7 Lives, death, respawn, game over — `GameSession.cs` ✅

- 3 lives. On death: if lives > 1, decrement and **reload the current scene** (the player respawns at the level start). Otherwise reset the session: destroy `ScenePersist`, load **build index 0**, destroy the session (score resets to 0, lives to 3).
- There are no checkpoints, no invulnerability frames, no game-over screen, and no feedback beyond the HUD number changing.

### 4.8 Level state persistence — `ScenePersist.cs` ✅

- Singleton `DontDestroyOnLoad` container. Each level's coins and enemies are **children** of its `Scene Persist` instance, so collected coins and killed enemies **stay gone after a death reload**.
- Destroyed on level exit and on game over. The duplicate created by a reloaded scene destroys itself in `Awake` (classic course pattern; it works, but the duplicate still lives for one frame).

### 4.9 Level completion — `LevelExit.cs` on `Exit Portal.prefab`

- Trigger on the Interactables layer, tinted `doorway-white.png`. On **any** trigger enter it waits `levelLoadDelay = 1 s` (real time), resets ScenePersist, then loads `buildIndex + 1`, wrapping to 0 after the last scene.
- No tag check, no exit animation or sound, and no input lock during the delay (the player can still move, shoot or die).

### 4.10 Camera ✅

- `Main Camera.prefab`: orthographic, size 4, `CinemachineBrain` (Smart Update).
- `State-Driven Camera.prefab` (Cinemachine 3): driven by the **player's Animator state**. Idle camera size 4, Run camera size 4.5, Ladder camera size **6** (zooms out while climbing). Each camera has a Position Composer and a Confiner2D bound to the Background tilemap's PolygonCollider2D.
- Blends (`Assets/Animations/Cinemachine Blender Settings.asset`): Idle→Run 0.3 s, Run→Idle 2 s, Any→Ladder 0.5 s, Ladder→Any 1 s.
- The confiner area extends about 2 rows above the playable ceiling (background-only tiles there). ⚠️ The player Rigidbody has no interpolation, which can cause jitter with camera follow. Runtime check needed.

### 4.11 HUD — `Game Session.prefab` ✅

- Screen Space Overlay canvas with **Constant Pixel Size** scaling (no resolution scaling). `Lives Text` top-left (green), `Score Text` top-right (gold), both TMP using `Symtext SDF`. Plain numbers with no labels or icons. Placeholder text `99` / `999999` is overwritten in `Start`.

### 4.12 Summary matrix

| System | Status |
|---|---|
| Run / flip | ✅ Implemented |
| Jump | ✅ Implemented (basic, no game-feel features) |
| Ladder climb | ✅ Implemented |
| Bounce pad | ✅ Implemented (physics material only) |
| Shooting | 🟠 Implemented, layer/interaction bugs |
| Enemy patrol | 🟠 Implemented, fragile turning |
| Enemy death | 🟠 Partial (trigger-side only) |
| Hazards (spikes, water) | ✅ Implemented (feet-only) |
| Coins / score | 🟠 Implemented, relies on per-instance overrides |
| Lives / respawn | 🟠 Implemented, death feedback skipped |
| Level persistence | ✅ Implemented |
| Level exit / progression | 🟠 Implemented, no tag check, endless loop |
| HUD | 🟡 Minimal |
| Unused input actions (Look, Interact, Crouch, Sprint, Prev/Next) | ⚪ Unused |
| `Bullets` layer | ⚪ Configured but unused |
| Menus, pause, settings, save, game over, victory, music | ❌ Absent |

---

## 5. Visual and audio asset inventory

### 5.1 Art style ✅

A pixel-art fantasy platformer look at 32 PPU with point filtering. The `SPA_*` assets are stylistically consistent with each other (Foxfin *Super Platformer Assets*). The non-SPA assets (white doorway with a teal tint, "shooting star" projectile, spikes, mushroom) come from different sources, so their consistency should be judged visually. The doorway uses a different resolution and PPU (128 px @ 100 PPU).

### 5.2 Sprites ✅

| File | Size (px) | Slices | Used for | Origin |
|---|---|---|---|---|
| `Sprites/SPA_Player_Ginger.png` | 376×228 | 30 | Player (17 frames referenced by clips; the rest unused) | Foxfin SPA (per README) |
| `Sprites/SPA_Enemy_Right_Red.png` | 240×40 | 7 | Enemy walk (6 frames) | Foxfin SPA |
| `Sprites/SPA_Coins.png` | 452×32 | 13 | Coin spin (12 frames) | Foxfin SPA |
| `Sprites/SPA_Rock_Grass_Water.png` | 184×222 | 31 | Rule Tile platforms, water tiles 28/29 | Foxfin SPA |
| `Sprites/SPA_Background.png` | 148×148 | 17 | Background tilemap (16 tiles) | Foxfin SPA |
| `Sprites/SPA_Ladder.png` | 32×32 | 1 | Ladder tile | Foxfin SPA |
| `Sprites/Spikes.png` | 32×32 | 1 | Spike tile | ❓ Not documented |
| `Sprites/TileVania+Three+Mushroom.png` | 32×32 | 1 | Bounce mushroom | ❓ Likely course-supplied (name), not documented |
| `Sprites/Shooting Star.png` | 58×65 | 1 | Projectile | ❓ Not documented |
| `Sprites/doorway-white.png` | 128×128 | 1 | Exit portal | ❓ Not documented |

`Sprites/Materials/*.mat` (`SPA_Ladder`, `Shooting Star`, `doorway-white`) are sprite materials.

### 5.3 Backgrounds and parallax

- A single static **tiled** background layer (Background tilemap). **No parallax** and no skybox or gradient. Both levels use the identical background.

### 5.4 Animations and VFX ✅

| Clip | Frames @ 12 fps | Loop |
|---|---|---|
| `Idling.anim` | 4 | yes |
| `Runnung.anim` (sic) | 10 | yes |
| `Climbing.anim` | 2 | yes |
| `Dying.anim` | 1 | no |
| `Enemy.anim` | 6 | yes |
| `Coin Spin.anim` | 12 | yes |

There is **no particle or VFX** (no dust, hit, pickup sparkle or death effect), no screen shake, and no lighting beyond the global light.

### 5.5 UI and typography

- HUD only (see 4.11). No title, menus, pause, options, game-over or victory screens.
- Fonts: `Assets/Fonts/Symtext.ttf` → `Symtext SDF.asset`. The font's own metadata says *"Symtext © (Neale Davidson). 2012. All Rights Reserved"*. **The license is not included in the project** ❓. `LiberationSans` (TMP default, OFL license file present) is also available.
- `TextMesh Pro/Sprites/EmojiOne.png` (TMP sample, attribution file present) is unused by the game.

### 5.6 Audio ✅

| Asset | Used by | Origin |
|---|---|---|
| `Audio/Coin-Pickup.ogg` | `CoinPickup.cs` via `AudioSource.PlayClipAtPoint` | ❓ Not documented |

- **No music, no AudioMixer, no other SFX** (jump, land, shoot, enemy hit/death, player hurt/death, exit, bounce, climb, UI).
- Playback architecture: a fire-and-forget `PlayClipAtPoint`, which creates a temporary **3D** AudioSource at the coin's position. The Audio Listener is on the camera 10 units away on Z. ⚠️ The pickup may play noticeably quieter than intended. Runtime check needed.
- No volume settings.

### 5.7 Licensing summary (important) ✅ / ❓

| Asset group | Status |
|---|---|
| All `SPA_*` sprites and tiles derived from them | **Restricted.** `Sprites/README - License details.txt`: provided via GameDev.tv's *Complete Unity Developer 2D* course, from Foxfin's *Super Platformer Assets* (Unity Asset Store #42013). Personal/student license only: **"you CANNOT use them in a published game"** without buying the pack. |
| `Spikes.png`, `TileVania+Three+Mushroom.png`, `Shooting Star.png`, `doorway-white.png` | ❓ Unknown origin and license. Do not assume they are free to publish. |
| `Coin-Pickup.ogg` | ❓ Unknown origin and license. |
| `Symtext.ttf` | ❓ "All Rights Reserved" in the font metadata. License terms are not in the project. |
| TMP LiberationSans | OFL (license file included). Fine. |
| Code (7 scripts) | Written in this project following course material. Treat as yours, but it mirrors course patterns. |
| Hot Reload package | Third-party dev tool. Check its license terms separately. |

---

## 6. Architecture and dependencies

### 6.1 Script inventory ✅

| Script | Lines | Responsibility | Depends on |
|---|---|---|---|
| `Scripts/PlayerMovement.cs` | ~100 | Input handling, run, jump, climb, shoot, death detection, animator params | Input System, `Bullet` prefab, `Gun` transform, `GameSession` (found at runtime), layer names `Ground`/`Ladder`/`Enemy`/`Hazards` |
| `Scripts/EnemyMovement.cs` | ~30 | Constant patrol, flip on trigger exit | — (stray `using Unity.VisualScripting`) |
| `Scripts/Bullet.cs` | ~35 | Projectile motion, enemy kill | `PlayerMovement` (found at runtime), tag `Enemy` |
| `Scripts/CoinPickup.cs` | ~20 | Pickup, score, SFX | `GameSession` (found at runtime), tag `Player` |
| `Scripts/GameSession.cs` | ~60 | Singleton: lives, score, HUD text, death/reset flow | `ScenePersist`, TMP, SceneManager build indices |
| `Scripts/ScenePersist.cs` | ~20 | Singleton container for per-level coins/enemies | — |
| `Scripts/LevelExit.cs` | ~25 | Delayed next-scene load | `ScenePersist`, build order |

### 6.2 Relationships

```
PlayerInput ──SendMessage──▶ PlayerMovement ──Instantiate──▶ Bullet ──Destroy──▶ Enemy
                                   │ (touch Enemy/Hazards)
                                   ▼
CoinPickup ──AddToScore──▶ GameSession (DontDestroyOnLoad, owns HUD canvas)
                                   │ TakeLife → reload scene / Reset → load index 0
                                   ▼
LevelExit ──ResetScenePersist──▶ ScenePersist (DontDestroyOnLoad, parents coins+enemies)
Animator state ──▶ CinemachineStateDrivenCamera (Idle/Run/Ladder cams)
```

All cross-object links are resolved at runtime with `FindFirstObjectByType` / `FindAnyObjectByType`. There are no events, interfaces, ScriptableObjects or service layer.

### 6.3 Scene / prefab dependencies

- Every level **must** contain `Game Session`, `Scene Persist` (with coins/enemies parented under it), `Player` (with the scene-level `Player` tag override), the cameras with the confiner wired to that scene's Background collider, and an `Exit Portal`.
- The camera prefab's `Target.TrackingTarget`, `AnimatedTarget` and `BoundingShape2D` are all **scene-level overrides**. A new level needs them rewired by hand.

### 6.4 Hardcoded values and fragile references ✅

- String layer lookups every frame: `LayerMask.GetMask("Ground" | "Ladder" | "Enemy" | "Hazards")`.
- String tags: `"Player"`, `"Enemy"`. String animator params: `"isRunning"`, `"isClimbing"`, `"Dying"`.
- Build-index logic: game over → index **0**. Exit → index + 1 with wrap. Adding a menu at index 0 would change both.
- Gameplay tuning lives in per-prefab serialized fields (speeds, lives, points), which is good, but there is no central config.
- Correct behavior depends on **per-instance overrides** (Player tag, coin triggers) rather than prefab defaults.
- Physics velocity is written in `Update` rather than `FixedUpdate`.
- Unused `using` directives: `UnityEngine.UIElements` (PlayerMovement), `Unity.VisualScripting` (EnemyMovement), `System` (GameSession).
- Typo in asset name: `Runnung.anim`.

### 6.5 Obsolete APIs

None found. The code already uses Unity 6 APIs (`linearVelocity`, `FindFirstObjectByType`, `FindObjectsByType`), and the current Editor log has no script warnings.

---

## 7. Existing issues and risks (prioritized)

Severity: 🔴 Critical/blocker · 🟠 High · 🟡 Medium · 🟢 Low.

| # | Sev | Issue | Evidence | Confidence |
|---|---|---|---|---|
| I-1 | 🔴 | **Most art cannot be used in a published game** under the current license; several other assets have unknown licenses. | `Assets/Sprites/README - License details.txt`; §5.7 | ✅ Confirmed |
| I-2 | 🟠 | **Death feedback is skipped.** `PlayerMovement.Die()` triggers the animation and kick, then immediately calls `ProcessPlayerDeath()`, which calls `SceneManager.LoadScene` with no delay. The dying animation and knockback are effectively never visible. | `PlayerMovement.cs:92-100`, `GameSession.cs:30-55` | ✅ code / ⚠️ visual |
| I-3 | 🟠 | **Bullets can trigger the Exit Portal.** The bullet is on the Player layer, which collides with Interactables, and `LevelExit` has no tag check. Shooting the portal should load the next level. | `Bullet.prefab` layer 8; collision matrix; `LevelExit.cs:8-11` | ⚠️ Very likely |
| I-4 | 🟠 | **Bullets interact with the player and the world wrongly.** They are on the Player layer (which collides with itself), so when firing **left** the spawn point (x −0.26) lies inside the player capsule (left edge ≈ −0.28), which can push the player or stop the bullet. Bullets also vanish on ladder/coin/spike triggers. The sprite is not flipped. There is no cooldown. | `Bullet.prefab`, `Player.prefab` Gun/capsule values, `Bullet.cs` | ⚠️ Needs runtime check |
| I-5 | 🟠 | **Prefab defaults are wrong; scenes patch them.** `Player.prefab` is `Untagged` and `Coin.prefab` has a non-trigger collider. A freshly dragged-in Player or Coin makes coins uncollectable (and solid). | Prefab YAML vs. scene overrides | ✅ Confirmed |
| I-6 | 🟠 | **No end state.** Finishing Level 2 loops to Level 1 with score and lives kept. Game over silently restarts Level 1. | `LevelExit.cs:16-23`, `GameSession.cs:42-47` | ✅ Confirmed |
| I-7 | 🟡 | **Exit may trigger twice.** The player has two colliders (capsule + feet), so two coroutines start, each calling `ResetScenePersist` and `LoadScene`. | `LevelExit.cs` | ⚠️ Needs runtime check |
| I-8 | 🟡 | **Enemy turning is fragile.** It reverses on *any* trigger exit. Kinematic bodies pass through walls, so turning depends on the ledge sensor alone. | `EnemyMovement.cs:21-25`, `Enemy.prefab` | ⚠️ Needs runtime check |
| I-9 | 🟡 | **Enemy kills depend on hit side.** Only the front trigger sensor registers a kill; hits on the solid capsule only delay-destroy the bullet. | `Bullet.cs`, `Enemy.prefab` | ⚠️ Needs runtime check |
| I-10 | 🟡 | **Jump adds to existing Y velocity**, so jump height can vary (e.g. on upward motion, mushroom contact). No coyote time, buffering or variable height. | `PlayerMovement.cs:46-52` | ✅ code / ⚠️ feel |
| I-11 | 🟡 | **Hazards kill only via the feet collider.** Walking sideways into spikes or brushing them with the head does not kill. Water is a solid collider. | `PlayerMovement.cs:93`, Water tilemap | ✅ Confirmed |
| I-12 | 🟡 | **HUD does not scale** (Constant Pixel Size), has no labels, and the duplicate session canvas exists for one frame on reload. | `Game Session.prefab`, `GameSession.cs` | ✅ / ⚠️ |
| I-13 | 🟡 | **Uncommitted change** in `Assets/Fonts/Symtext SDF.asset` (Editor-generated atlas change). | `git status` | ✅ Confirmed |
| I-14 | 🟢 | **`Assets/_Recovery/`** holds 3 crash-recovery scenes. `0 (2).unity` (Jan 20, after the last commit) has the same tile counts and objects as `Level 2.unity` and is probably redundant, but a diff is advised before cleanup. | `Assets/_Recovery/*` | ✅ / ❓ |
| I-15 | 🟢 | Level 2 has no `EventSystem` (harmless now, matters once UI is interactive). | Level 2 scene | ✅ Confirmed |
| I-16 | 🟢 | No Rigidbody interpolation on the player, so camera jitter is possible. | `Player.prefab` | ⚠️ Needs runtime check |
| I-17 | 🟢 | Unused packages (Visual Scripting, Timeline, Aseprite, PSD Importer, SpriteShape, 2D Animation, Multiplayer Center, Collab), unused input actions, unused `Bullets` layer, unused AudioSource on Coin. | manifest, prefabs | ✅ Confirmed |
| I-18 | 🟢 | Unity 6.2 (non-LTS) plus a Git-URL third-party package. Long-term maintenance and reproducibility risk. | `ProjectVersion.txt`, manifest | ✅ Confirmed |
| I-19 | 🟢 | Coin SFX is played as 3D audio 10 units from the listener and may be quiet. | `CoinPickup.cs:15` | ⚠️ Needs runtime check |

---

## 8. Reusable foundations

| Foundation | Reuse value | Notes |
|---|---|---|
| Tilemap structure (Grid with Platforms, Ladder, Hazards, Water, Bouncing, Background) | **High** | A clean separation by physics layer and sorting layer. Works with any replacement tileset. |
| Layer and sorting-layer scheme + 2D collision matrix | **High** | Already includes an unused `Bullets` layer. Needs small fixes only. |
| Rule Tile workflow | **High** (pattern) | The rules must be redone for a new tileset. |
| Cinemachine 3 state-driven camera + Confiner2D | **High** | Modern setup. Zoom-on-ladder is a nice touch to keep or extend. |
| Input System action asset + `PlayerInput` | **High** | Already supports keyboard and gamepad. Could move from Send Messages to C# events or generated classes. |
| URP 2D Renderer + Light2D | **High** | Ready for 2D lights, shadows, and post-processing. |
| Prefab set (Player, Enemy + variant, Coin + variant, Exit, Session, Persist, Cameras) | **Medium** | Good starting points. Fix the defaults (I-5). |
| Gameplay scripts | **Low–Medium** | Small and readable, a good *reference* for behavior, but most would be rewritten for game feel, events and robustness. `ScenePersist`'s "keep collected items after death" rule is worth preserving as a design decision. |
| Level layouts | **Medium** | Useful as greybox and teaching levels. Small (one room each). |
| Art and audio | **Prototype only** | Licensing restrictions (§5.7). |

---

## 9. Potential modernization opportunities (suggestions only)

These are options for discussion, not decisions.

**Game feel and controls**
- Acceleration/deceleration curves, coyote time, jump buffering, variable jump height, apex hang, fall-speed cap.
- Jump, fall, land, shoot and hurt animations. Squash and stretch, dust and landing particles.
- Ladder top/bottom snapping, jump-off-ladder, drop-through platforms.

**Combat and enemies**
- A shooting cooldown, a projectile pool, and proper layer use. Hit-flash, knockback, an enemy death animation and score.
- Enemy variety (patrol, chase, ranged, flying), raycast-based ledge/wall detection, a stomp-to-kill option.

**Health and progression**
- A health/hearts model instead of one-hit deaths. Invulnerability frames, checkpoints, a death sequence with delay and transition.
- Title screen, level select, pause, settings (audio, controls, display), game-over and victory screens, saved progress and best scores.
- A data-driven level list (ScriptableObject) instead of raw build order.

**World and visuals**
- A licensed or original art set with a coherent style. Multi-layer parallax backgrounds, animated tiles (water, torches), 2D lights and post-processing.
- Larger, scrolling, multi-section levels. Secrets, collectibles with purpose (unlocks, keys), moving platforms, breakable blocks.
- A resolution-independent UI (Scale With Screen Size) with icons, labels and juice (tweening).

**Audio**
- A music track per level or theme, a full SFX set, an AudioMixer with music/SFX buses, and a central audio service (2D playback for UI and pickups).

**Architecture**
- Namespaces + assembly definitions. Event-driven communication (C# events or ScriptableObject channels) instead of `Find*`.
- Config ScriptableObjects for tuning. Cached layer masks and animator hashes. Physics in `FixedUpdate`.
- Remove unused packages. Decide on an LTS Unity version. Add a basic test or validation pass (e.g. edit-mode checks that every level has the required objects).

---

## 10. Open questions for you

1. **Commercial intent:** Do you plan to publish (free or paid), or is this a portfolio/personal project? This decides whether all `SPA_*` art, the font, the SFX and the other undocumented assets must be replaced or licensed.
2. **Do you own the Super Platformer Assets pack** (Asset Store #42013) or other art packs you would like to use? Do you know where `Spikes.png`, `Shooting Star.png`, `doorway-white.png`, the mushroom, `Coin-Pickup.ogg` and `Symtext.ttf` came from?
3. **Art direction:** Keep 32-PPU pixel art, or change resolution or style? Original art, purchased packs, or a mix?
4. **Unity version:** Stay on 6000.2.13f1, or move to an LTS release (e.g. Unity 6.3 LTS) before development starts?
5. **Hot Reload:** Keep this third-party Git package as a team dependency?
6. **Core design:** Keep shooting as the main combat? One-hit deaths or health? Keep "collected coins stay collected after death"?
7. **Scope:** How many levels or worlds, what length per level, and which new mechanics (moving platforms, keys and doors, double jump, dash, wall jump)?
8. **Target platforms and input:** PC only? Web? Mobile/touch? Gamepad as first-class?
9. **Housekeeping (when implementation starts):** Commit or discard the pending `Symtext SDF.asset` change? Delete `Assets/_Recovery/` after diffing `0 (2).unity` against `Level 2.unity`?
10. **Name and identity:** Keep the name "TileVania" (shared with the course project) or rebrand? Update company/product metadata?
11. **Existing levels:** Preserve Level 1 and Level 2 as tutorial levels, rework them, or replace them entirely?

---

*End of audit. Nothing in the project was modified except creating this document (`Docs/CURRENT_STATE_AUDIT.md`, outside `Assets/`, so Unity will not import it; it will show as untracked in Git).*
