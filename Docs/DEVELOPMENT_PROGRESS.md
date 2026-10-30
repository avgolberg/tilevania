# Development Progress

**Branch:** `modernization`. **Recovery point:** tag `pre-modernization` on `1921e3d`, the original course project. Pushed to `origin` (GitHub) on 2026-10-30, with `main` fast-forwarded to `modernization`. **Unity:** 6000.2.13f1, URP 2D, Input System. No new packages were added.

## Iteration 2: controls and gameplay fixes

| Issue | Root cause | Fix (survives Rebuild Game) |
|---|---|---|
| Could not climb down the Level 1 shaft ladder | **(1)** The hero passes wooden ledges by turning off Player↔OneWay collisions in the layer matrix. But `PlatformEffector2D` defaults to its own "collide with everything" mask (`useColliderMask = true`), so the matrix was ignored. On the shaft's ladder-top ledge, climbing started but the hero never moved. **Down + Jump drop-through was broken for the same reason.** **(2)** Grabbing from above tested a single point under the hero's centre, so standing slightly off-centre over the 1-tile shaft never grabbed it. | `LevelBuilder`: `useColliderMask = false`. `PlayerController`: ladder detection with 0.3-tile tolerance, evaluated at cell centres (no edge ambiguity), and snapping to the detected column. |
| (found while testing) Climbing **down** a ladder that stands on a wooden ledge (Level 2 watchtower) fell through the ledge | The bottom-exit rule only recognised *solid* ground | Exit when no ladder is under the feet and *any* ground (solid or one-way) is. Ledge collision is restored immediately. |
| Objects in Level 2 looked embedded in the floor | **Sorting, not transforms.** The castle floor's top lip (and the forest grass and flowers) was on the `Foreground` sorting layer, so it was drawn over the feet of everything standing on the floor. Spikes were half hidden. | The decor tilemap is now `SurfaceDecor` on the `Platforms` layer (behind all gameplay objects). |
| Chest sprite bounds extended below the floor | **Transform/slicing:** the 48×48 chest frames have 12 transparent rows under the art | Chests are sliced to the art (48×24) with the pivot at the true bottom. |
| Floating pickups could dip into ledges while bobbing | Placed at the cell centre regardless of sprite size or bob amplitude | `LevelBuilder.PickupPosition` computes the height from sprite bounds plus bob amplitude. |
| Spiders could bob into the ceiling | Body placed at the layout cell, ignoring the 1-unit bob | Placement keeps the whole bob below the anchor. |
| Two castle windows sat behind wooden ledges | A side effect of lowering ledges in iteration 1 | Windows moved up in the layout. |
| "Found the Gold Key!" visible in every screenshot | **Screenshot capture artifact:** the capture tool deliberately injected that toast to show the HUD. The runtime lifecycle was fine: toasts fade after about 3 s, and each scene has its own HUD. | The capture tool no longer fakes a toast. A test now verifies fade-out and that nothing carries into the next scene. |

**New controls:** see `GAME_DESIGN.md` §1. Arrow keys plus Z/Space/X/Esc, context-sensitive Up, no mouse in gameplay, gamepad kept.

**Validation (iteration 2):** clean compile; validator 0 problems; **39/39 Play Mode tests pass**; reachability check OK; Windows build succeeded; regenerated screenshots reviewed.

## Completed

| Area | Status |
|---|---|
| Preparation | Audit read; downloads inspected; recovery tag and branch created; licenses checked (see `ASSET_CREDITS.md`) |
| Asset import | Approved art and audio copied into `Assets/_Game` and configured consistently: point filter, uncompressed, no mips, explicit PPU, custom pivots, stable sprite IDs |
| Player | Acceleration movement, coyote time, jump buffer, variable jump, ladders, one-way drop-through, forward and upward shooting, 3 hearts, knockback, i-frames, death sequence, pits, juice (dust, squash, sounds) |
| Enemies | Spikes, slime, spider, skeleton (raycast patrol), ghost (telegraphed ranged), bat (flying). Single direction-independent hurtbox |
| Combat fixes from the audit | Dedicated projectile layers; projectiles can't touch the player, pickups or exits; spawn-in-wall guard; layer matrix enforced in code and project settings; no tag- or trigger-exit-based logic |
| Items and progression | Gems, hearts, gold and treasure keys, gates and doors, iron/silver/gold chests, checkpoints, animated portals, explicit scene flow (no looping) |
| Levels | Forest + underground (128×30), Castle (140×34), Treasure Chamber (40×22), built from ASCII layouts with auto-tiling, back-walls, decor, lights and parallax |
| Boss | Dragon "Guardian of the Treasure": 3 telegraphed attacks, enrage phase, health bar, hit and defeat effects, treasure reveal, full reset on retry |
| UI | Main menu (Play/Settings/Credits/Quit), HUD, pause, death and victory panels, boss bar, level title cards, toasts, fades |
| Audio | Cross-fading music per area, 24 mapped SFX, independent saved Music/SFX volume |
| Tooling | Batch-capable builder (`Tools → TileVania → …`), scene validator, screenshot capture, Windows player build, Python level generator and reachability checker (`Tools/LevelTools`) |
| Docs | `GAME_DESIGN.md`, `ASSET_CREDITS.md`, `QA_CHECKLIST.md`, this file, `Docs/Screenshots/` |

## Verification actually performed

| Check | Result |
|---|---|
| C# compilation (batch mode) | ✅ 0 errors, 0 project warnings |
| Full regeneration (`GameBuildPipeline.BuildAllBatch`) | ✅ Completes. The validator reports **0 problems**: no missing scripts or references, non-empty collider geometry, every `nextScene` in the build list, managers and EventSystem present in every scene |
| Build settings | ✅ `MainMenu`, `Level1_Forest`, `Level2_Castle`, `Level3_TreasureChamber` (old course scenes removed from the build) |
| **Automated Play Mode tests** (real scenes, simulated input) | ✅ **39 / 39 pass** (iteration 2 added controls, ladder, placement and toast tests). Iteration 1 tests: spawn and run, jump height and landing, ladder climb to a ledge, forward shot kills the slime, the skeleton dies from either side, upward shot, gem pickup, spikes cost exactly 1 heart, pit respawn, death panel and retry, gate blocks then opens with the key, **shots don't trigger the exit**, portal loads the castle, treasure door needs the right key, boss starts/dies/reveals the treasure/shows victory, **boss beaten with real projectiles in under 30 s of simulated play**, boss resets on retry, menu Play loads Level 1, all scenes run without logged errors |
| Layout reachability (static solver) | ✅ Every key, gate, portal and treasure is reachable in order. It found, and I fixed, ledges and boss platforms that were 4–5 tiles high (the jump is 3.5) |
| Windows standalone build | ✅ Succeeded (110 MB). A 10 s headless run of the built `.exe` logged no errors |
| Visual review | ✅ Rendered screenshots of every area, the HUD, the pause menu, the boss bar and the victory panel were reviewed and fixed iteratively (`Docs/Screenshots/`) |

**Not performed:** no human hands-on play-testing. I could not drive the interactive Editor, so game *feel*, difficulty, audio mix and readability at speed still need your manual pass (`QA_CHECKLIST.md`). The gamepad bindings exist but are untested.

## Known limitations / remaining work

1. **Licenses to confirm** for four OpenGameArt packs and two music tracks. OpenGameArt blocked automated access; see the action items in `ASSET_CREDITS.md`.
2. **Missing animation frames in the hero pack:** climbing loops the jump frames, hurt uses one apex frame, death is procedural (spin and fade). Fine for now; a PRO pack or custom frames would improve it.
3. **Mixed pixel density:** the enemy sheets are higher resolution than the 16 px tiles (shown at 80 PPU). They're consistent in size but not in pixel grid.
4. **SFX picks** were chosen by file name from the Kenney packs, so some may not suit. Swap them in `Resources/SfxLibrary.asset` without touching code.
5. **Pixel scale** is exact at 1080p (5×). Other resolutions use a non-integer scale, so slight shimmer is possible. A Pixel Perfect Camera could be added later.
6. **The builder overwrites the generated scenes and prefabs.** For hand-edited scenes, either stop using Rebuild or put the changes in the layouts and builder code.
7. Difficulty is untuned by humans. The boss has 30 HP, and the simulated bot (with healing) beat it in under 30 s. Real players will take longer, but it may still need a tuning pass.
8. Nice-to-haves not done: save/continue, level select, footstep sounds, gamepad UI testing, a Pixel Perfect Camera, rebindable controls.
9. The project still has the original packages, including the Git-hosted Hot Reload and the now-unused Cinemachine (still referenced by the legacy prefabs). They weren't touched because of the "avoid changes not requested" rule.

## Decisions for your review

- **Boss as a dedicated scene** (`Level3_TreasureChamber`). This makes respawn and reset logic simpler and keeps Level 2 smaller.
- **Glax's dragon is the Guardian.** It is the only creature in the approved packs large enough for a boss.
- **Level geometry comes from ASCII layouts plus an editor builder**, not hand-painted tilemaps. This made the levels verifiable, reproducible and testable from the CLI. The scenes are normal Unity scenes and can still be edited afterwards.
- **A custom camera script replaced Cinemachine** for exact bounds clamping, pixel snapping and screen shake with no extra wiring.
- **Legacy content moved** (`git mv`, GUIDs preserved) to `Assets/_Legacy`. The original scenes are still there; the crash-recovery scenes in `Assets/_Recovery` are untouched.
- **Ladder art is generated** because none of the approved packs has a ladder or rope.
- The **Symtext font change** (pre-existing, unrelated) is left uncommitted, as requested.
