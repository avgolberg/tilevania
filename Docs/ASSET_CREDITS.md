# Asset Credits & Licensing

All third-party assets were taken from the user-approved downloads in `D:\Unity Projects\TileVania_AssetDownloads`. The original archives and folders there were **not modified**. Copies used by the game live in `Assets/_Game/Art` and `Assets/_Game/Audio`. License or readme files that shipped with a pack are copied to `Assets/_Game/Art/Licenses/`.

**How each license was verified.** OpenGameArt blocks automated page fetches (HTTP 403), so pages could not be read directly. Each row states the evidence used:

- ✅ **Bundled file**: a license or readme file inside the download states the terms.
- 🔎 **Search result**: a web search snippet of the official page states the terms.
- ⚠️ **Unconfirmed**: no license text was available offline. **Please open the URL and confirm before any public release.**

## Used in the game

| Asset | Author | Source | License | Evidence | Used for | Modifications |
|---|---|---|---|---|---|---|
| Adventurer Hero (FREE) | STAEL | https://stael-l.itch.io/adventurer-hero | Custom: personal **and commercial use allowed**, modification allowed, credit appreciated but **not required**. **No redistribution or resale** of the assets themselves. | 🔎 itch.io page (fetched directly) | Player (idle, run, jump, apex, fall, attack) and the slash projectile (from `Effects/attack_1.png`) | Sliced; the projectile is a crop of one effect frame. Climb and hurt reuse existing frames. |
| Pixel Art Platformer Asset Pack | Glax (mattbas) | https://opengameart.org/content/pixel-art-platformer-asset-pack | **CC0** (credit as "Glax" optional) | ✅ `README.txt` | Forest tiles, spikes, column, fire, grass and flowers, bushes and leaf clumps, tree trunk, clouds, stars, mountains, moon (castle), **the dragon boss**, flame projectiles | Sliced and tinted at runtime; the dragon is shown at 2× |
| Spooky Castle Tileset (`oppcastle-tiles.png`) | see OGA page | https://opengameart.org/content/spooky-castle-tileset | ⚠️ unconfirmed | ⚠️ none bundled | Castle bricks, battlement back-walls, wooden bridges, torch, windows, fence gate, wooden door | Sliced; back-wall tinted |
| 2D Platformer Enemies (`plat_*_spritesheet.png`) | see OGA page | https://opengameart.org/content/2d-platformer-enemies | ⚠️ unconfirmed | ⚠️ none bundled | Skeleton, slime, ghost, bat, spider | Sliced |
| 2D Cave Platformer Tileset 16×16 | Rotting Pixels | https://opengameart.org/content/2d-cave-platformer-tileset-16x16 | ⚠️ unconfirmed (bundled txt has no license text) | ⚠️ `RottingPixels.txt` (credits only) | **Gems only**, as instructed | Sliced (4 gem cells) |
| CC0 Key Icons | 7Soul1 | https://opengameart.org/content/cc0-key-icons | CC0 (per the page title) | ⚠️ bundled txt only names the DeviantArt source | Gold key, Treasure key (HUD and pickups) | none |
| Animated Pixel Art Treasure Chests | Yevhen Babiichuk (DustDFG) | https://opengameart.org/content/animated-pixel-art-treasure-chests | **CC-BY 4.0** — **attribution required** | ✅ `README.md` + `CC-BY-4.0.txt` | Iron, silver and gold chests (3-frame open animation) | Sliced |
| Portals (`portalRings1/2.png`) | see OGA page | https://opengameart.org/content/portals | ⚠️ unconfirmed | ⚠️ none bundled | Exit portal (opening animation and loop) | Sliced |
| 8-bit Perilous Dungeon | HydroGene | https://opengameart.org/content/8-bit-perilous-dungeon | **CC0** | 🔎 search snippet of the OGA page | Menu and Level 1 music | none |
| 8-bit Slay the Evil | HydroGene | https://opengameart.org/content/8-bit-slay-the-evil | Page says "credit not required"; license field not seen (sibling tracks are CC0) | 🔎 partial ⚠️ | Boss music | none |
| Spooky Dungeon | Memoraphile (You're Perfect Studio) | https://opengameart.org/content/spooky-dungeon | Listed in the author's "Memoraphile CC0" collection. The page says it is free to use with or without attribution. | 🔎 partial ⚠️ | Level 2 and pre-boss music | none |
| Impact Sounds | Kenney | www.kenney.nl | **CC0** | ✅ `License.txt` | Land, enemy hit/die, hurt, unlock, boss hit/die, rock impact, warning bell | Renamed copies |
| Interface Sounds | Kenney | www.kenney.nl | **CC0** | ✅ `License.txt` | Jump, shoot, gem, key, chest, locked, checkpoint, portal, UI, victory, heal | Renamed copies |
| Liberation Sans (TextMesh Pro default) | Liberation Fonts project | bundled with TMP | SIL OFL 1.1 | ✅ license in TMP folder | All UI text | none |

### Required attribution text (CC-BY)

> Treasure chest sprites: "Animated Pixel Art Treasure Chests" by Yevhen Babiichuk (DustDFG), licensed under CC BY 4.0 — https://opengameart.org/content/animated-pixel-art-treasure-chests

The in-game **Credits** screen (Main Menu → Credits) lists every pack above, including this attribution.

## Project-made assets

Drawn from code in `Assets/_Game/Scripts/Editor/GeneratedArt.cs`, so they are original to this project:

- `Generated/Heart_Full.png`, `Heart_Empty.png`: 9×9 heart icons.
- `Generated/Ladder.png`: 16×16 wooden ladder tile. None of the approved packs includes a ladder or rope.
- `Generated/Pixel.png`: a 4×4 white pixel for particles, warning markers and UI fills.

## Not used: course-restricted and unknown-origin assets

Everything from the original course project was moved to `Assets/_Legacy/` and is **not referenced by the new game or included in the build**. That includes the Foxfin *Super Platformer Assets* (student license, no publishing), `Spikes.png`, `Shooting Star.png`, `doorway-white.png`, the mushroom, and `Coin-Pickup.ogg`. The `Symtext` font ("All Rights Reserved") stays in `Assets/Fonts/` untouched and is not used by the new UI.

Not used from the downloads: the Cave tileset's environment tiles (as instructed), Glax's UI frames, items, signs and ruins tiles, and the castle "mod" tile variant.

## Action items before a public release

1. Open the four ⚠️ OpenGameArt pages (castle, enemies, cave gems, portals) and record the license and author in the table above.
2. Confirm the license field on the *Slay the Evil* and *Spooky Dungeon* pages.
3. If any page requires attribution, add it to the Credits text in `UIBuilder.BuildMainMenuCanvas`, then rebuild.
