using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace TileVania.EditorTools
{
    /// <summary>
    /// Turns an ASCII layout (Assets/_Game/Levels/*.txt) into a complete, editable scene:
    /// auto-tiled tilemaps, props, enemies, pickups, lighting, parallax, camera, HUD and managers.
    /// Legend is documented in Docs/GAME_DESIGN.md.
    /// </summary>
    public static class LevelBuilder
    {
        public const string Scenes = ContentBuilder.Root + "/Scenes";
        const string Tiles = ContentBuilder.Root + "/Tiles";

        public class LevelSpec
        {
            public string layout, scene, title, subtitle, nextScene, music;
            public bool castle, boss;
        }

        class Layout
        {
            public int w, h, backwall = -1;
            public bool roofBackwall;
            public char[,] cells;
            public char At(int x, int y) => x < 0 || y < 0 || x >= w || y >= h ? '#' : cells[x, y];
            public bool Solid(int x, int y) => At(x, y) == '#';
            public bool OneWay(int x, int y) { char c = At(x, y); return c == '=' || c == 'I'; }
        }

        static Layout Load(string file)
        {
            var lines = File.ReadAllLines($"{ContentBuilder.Root}/Levels/{file}.txt");
            var header = lines[0];
            var rows = lines.Skip(1).Where(l => l.Length > 0).ToArray();
            var L = new Layout { h = rows.Length, w = rows.Max(r => r.Length) };
            L.cells = new char[L.w, L.h];
            for (int r = 0; r < L.h; r++)
                for (int x = 0; x < L.w; x++)
                    L.cells[x, L.h - 1 - r] = x < rows[r].Length ? rows[r][x] : '.';
            var bw = header.Split(' ').FirstOrDefault(t => t.StartsWith("backwall="));
            if (bw != null)
            {
                string v = bw.Substring("backwall=".Length);
                if (v == "roof") L.roofBackwall = true; else L.backwall = int.Parse(v);
            }
            return L;
        }

        // ------------------------------------------------------------------ tiles
        static readonly Dictionary<Sprite, Tile> tileCache = new Dictionary<Sprite, Tile>();

        public static Tile TileFor(Sprite s, Tile.ColliderType collider = Tile.ColliderType.None)
        {
            if (tileCache.TryGetValue(s, out var t) && t != null && t.colliderType == collider) return t;
            ContentBuilder.EnsureFolder(Tiles);
            string path = $"{Tiles}/{s.name}{(collider == Tile.ColliderType.Grid ? "_Solid" : "")}.asset";
            t = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (t == null)
            {
                t = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(t, path);
            }
            t.sprite = s;
            t.colliderType = collider;
            EditorUtility.SetDirty(t);
            tileCache[s] = t;
            return t;
        }

        static Sprite F(string n) => ArtSetup.GetSprite("Environment/Forest/Forest_Tiles.png", "Forest_" + n);
        static Sprite C(string n) => ArtSetup.GetSprite("Environment/Castle/Castle_Tiles.png", "Castle_" + n);

        static Tilemap MakeTilemap(GameObject grid, string name, string sortingLayer, int order, int layer, Color color)
        {
            var go = new GameObject(name) { layer = layer };
            go.transform.SetParent(grid.transform, false);
            var tm = go.AddComponent<Tilemap>();
            tm.color = color;
            var r = go.AddComponent<TilemapRenderer>();
            r.sortingLayerName = sortingLayer;
            r.sortingOrder = order;
            r.sharedMaterial = ContentBuilder.Lit;
            return tm;
        }

        static void AddSolidCollider(Tilemap tm, bool oneWay)
        {
            var go = tm.gameObject;
            var tc = go.AddComponent<TilemapCollider2D>();
            tc.compositeOperation = Collider2D.CompositeOperation.Merge;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
            var comp = go.AddComponent<CompositeCollider2D>();
            comp.geometryType = oneWay ? CompositeCollider2D.GeometryType.Outlines : CompositeCollider2D.GeometryType.Polygons;
            if (oneWay)
            {
                comp.usedByEffector = true;
                var eff = go.AddComponent<PlatformEffector2D>();
                eff.useOneWay = true;
                // Use the project's layer matrix (not the effector's own "everything" mask) so the player
                // can pass through while climbing or dropping (Physics2D.IgnoreLayerCollision Player/OneWay).
                eff.useColliderMask = false;
                eff.surfaceArc = 160f;
                eff.useSideFriction = false;
                eff.useSideBounce = false;
            }
        }

        // ------------------------------------------------------------------ build
        public static void Build(LevelSpec spec)
        {
            tileCache.Clear();
            var L = Load(spec.layout);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var rng = new System.Random(spec.layout.GetHashCode() & 0x7fffffff);
            bool castle = spec.castle;

            // --- Tilemaps
            var grid = new GameObject("Grid");
            grid.AddComponent<Grid>();
            var backTint = castle ? new Color(0.62f, 0.55f, 0.75f) : new Color(0.32f, 0.27f, 0.3f);
            var back = MakeTilemap(grid, "BackWall", "Background", 0, Layers.Background, backTint);
            var backDecor = MakeTilemap(grid, "BackDecor", "Background", 40, Layers.Background, Color.white);
            var ground = MakeTilemap(grid, "Ground", "Platforms", 0, Layers.Ground, Color.white);
            var oneWay = MakeTilemap(grid, "OneWay", "Platforms", 1, Layers.OneWay, Color.white);
            var ladder = MakeTilemap(grid, "Ladders", "Ladder", 0, Layers.Ladder, Color.white);
            // Surface decor (castle floor lip, grass, flowers) sits on the ground but behind every gameplay object.
            var front = MakeTilemap(grid, "SurfaceDecor", "Platforms", 2, Layers.Background, Color.white);
            AddSolidCollider(ground, false);
            AddSolidCollider(oneWay, true);
            var lc = ladder.gameObject.AddComponent<TilemapCollider2D>();
            lc.isTrigger = true;

            var ladderTile = TileFor(ArtSetup.SingleSprite("Generated/Ladder.png"), Tile.ColliderType.Grid);
            for (int x = 0; x < L.w; x++)
                for (int y = 0; y < L.h; y++)
                {
                    char ch = L.cells[x, y];
                    var cell = new Vector3Int(x, y, 0);
                    if (ch == '#')
                    {
                        ground.SetTile(cell, TileFor(GroundSprite(L, x, y, castle, rng), Tile.ColliderType.Grid));
                        // Underside tiles have a jagged transparent edge; fill behind them so the sky never shows through.
                        if (!castle && y > 0 && !L.Solid(x, y - 1) && L.roofBackwall)
                            back.SetTile(cell, TileFor(F("Fill_" + rng.Next(5))));
                        if (castle && !L.Solid(x, y + 1) && y + 1 < L.h)
                            front.SetTile(new Vector3Int(x, y + 1, 0), TileFor(C("Cap_" + (x & 1))));
                        continue;
                    }
                    if (ch == '=' || ch == 'I')
                        oneWay.SetTile(cell, TileFor(OneWaySprite(L, x, y, castle), Tile.ColliderType.Grid));
                    if (ch == 'H' || ch == 'I') ladder.SetTile(cell, ladderTile);

                    // Back wall
                    if (castle && y <= L.backwall)
                    {
                        int v = x & 3;
                        var s = y == L.backwall ? C("BackTop_" + v) : ((y & 1) == 0 ? C("BackFill_" + v) : C("BackFill2_" + v));
                        back.SetTile(cell, TileFor(s));
                    }
                    else if (L.roofBackwall && HasRoof(L, x, y))
                    {
                        back.SetTile(cell, TileFor(F("Fill_" + rng.Next(5))));
                    }
                }

            // Batch mode never runs the tilemap update loop, so bake collider shapes explicitly before saving.
            foreach (var tc in grid.GetComponentsInChildren<TilemapCollider2D>()) tc.ProcessTilemapChanges();
            foreach (var comp in grid.GetComponentsInChildren<CompositeCollider2D>()) comp.GenerateGeometry();

            // --- Objects
            var level = new GameObject("Level");
            var groups = new Dictionary<string, Transform>();
            Transform Group(string n)
            {
                if (!groups.TryGetValue(n, out var t))
                {
                    t = new GameObject(n).transform;
                    t.SetParent(level.transform, false);
                    groups[n] = t;
                }
                return t;
            }
            GameObject Place(string prefab, Vector2 pos, string group)
            {
                var src = ContentBuilder.LoadPrefab(prefab);
                if (src == null) throw new System.Exception("Missing prefab " + prefab);
                var go = (GameObject)PrefabUtility.InstantiatePrefab(src, scene);
                go.transform.SetParent(Group(group), false);
                go.transform.position = pos;
                return go;
            }
            GameObject Deco(string name, Sprite s, Vector2 pos, int order, Color tint, string layer = "Background")
            {
                var go = new GameObject(name);
                go.transform.SetParent(Group("Decor"), false);
                go.transform.position = pos;
                var sr = ContentBuilder.AddSprite(go, s, layer, order);
                sr.color = tint;
                return go;
            }

            GameObject player = null;
            Vector2 spawn = Vector2.zero;
            var gemNames = castle ? new[] { "Gem_Red", "Gem_Orange", "Gem_Blue" } : new[] { "Gem_Green", "Gem_Blue", "Gem_Red" };
            int gemIndex = 0;
            DragonBoss boss = null;
            TreasureChest finalChest = null;
            Vector2 arenaTrigger = Vector2.zero, arenaDoor = Vector2.zero;
            bool hasArena = false, hasDoor = false;

            for (int x = 0; x < L.w; x++)
                for (int y = 0; y < L.h; y++)
                {
                    char ch = L.cells[x, y];
                    var c = new Vector2(x + 0.5f, y + 0.5f);
                    var feet = new Vector2(x + 0.5f, y);
                    switch (ch)
                    {
                        case 'P': spawn = feet; break;
                        case 'C': Place("Checkpoint", feet, "Checkpoints"); break;
                        case 'g': { var n = gemNames[gemIndex++ % gemNames.Length]; Place(n, PickupPosition(n, x, y), "Pickups"); break; }
                        case 'h': Place("Pickup_Heart", PickupPosition("Pickup_Heart", x, y), "Pickups"); break;
                        case 'k': Place("Key_Gold", PickupPosition("Key_Gold", x, y), "Pickups"); break;
                        case 'K': Place("Key_Treasure", PickupPosition("Key_Treasure", x, y), "Pickups"); break;
                        case 'L': Place("Gate_Gold", new Vector2(x, y), "Gates"); break;
                        case 'D': Place("Gate_Treasure", new Vector2(x, y), "Gates"); break;
                        case 'c': Place("Chest_Iron", feet, "Chests"); break;
                        case '$': Place("Chest_Silver", feet, "Chests"); break;
                        case 'E': Place("ExitPortal", feet, "Exit"); break;
                        case 's': Place("Enemy_Skeleton", feet, "Enemies"); break;
                        case 'm': Place("Enemy_Slime", feet, "Enemies"); break;
                        case 'o': Place("Enemy_Ghost", c, "Enemies"); break;
                        case 'b': Place("Enemy_Bat", c, "Enemies"); break;
                        case 'p':
                        {
                            int top = y + 1;
                            while (top < L.h && !L.Solid(x, top) && !L.OneWay(x, top)) top++;
                            var sp = Place("Enemy_Spider", new Vector2(x + 0.5f, top), "Enemies");
                            // Keep the whole bob below the ceiling it hangs from.
                            var body = sp.transform.Find("Body");
                            var bodySr = body.GetComponentInChildren<SpriteRenderer>();
                            float reach = bodySr.sprite.bounds.max.y + Mathf.Abs(body.GetComponent<Bobbing>().amplitude.y) + 0.1f;
                            body.localPosition = new Vector3(0f, Mathf.Min(c.y - top, -reach), 0f);
                            break;
                        }
                        case '^':
                        {
                            var spike = Place("Hazard_Spikes", feet, "Hazards");
                            spike.GetComponent<SpriteRenderer>().sprite = F("Spike_" + rng.Next(3));
                            break;
                        }
                        case 't': Place("Torch", c, "Lights"); break;
                        case 'w': Deco("Window", C((x & 1) == 0 ? "WindowRound" : "WindowNarrow"), c, 45, backTint); break;
                        case ',':
                            if (castle) break;
                            if (rng.Next(3) == 0) Deco("Bush", ArtSetup.GetSprite("Environment/Forest/Forest_leaves.png", "Bush_Small"), feet + new Vector2(0, -0.1f), 45, Color.white);
                            else front.SetTile(new Vector3Int(x, y, 0), TileFor(F("Grass_" + rng.Next(2))));
                            break;
                        case '*': front.SetTile(new Vector3Int(x, y, 0), TileFor(F("Flower_" + rng.Next(2)))); break;
                        case '|': Deco("Column", F("Column"), feet, 42, new Color(0.85f, 0.85f, 0.9f)); break;
                        case 'A':
                            // ruined gateway: two broken columns flanking the entrance
                            Deco("Column", F("Column"), new Vector2(x + 0.5f, y), 42, new Color(0.85f, 0.85f, 0.9f));
                            Deco("Column", F("Column"), new Vector2(x + 3.5f, y), 42, new Color(0.85f, 0.85f, 0.9f));
                            Deco("ColumnTop", F("ColumnShort"), new Vector2(x + 3.5f, y + 1.5f), 42, new Color(0.85f, 0.85f, 0.9f));
                            break;
                        case 'B':
                        {
                            var b = Place("DragonBoss", new Vector2(x + 0.5f, L.h + 3f), "Boss");
                            boss = b.GetComponent<DragonBoss>();
                            break;
                        }
                        case 'T':
                        {
                            var t = Place("Chest_Gold", feet, "Boss");
                            finalChest = t.GetComponent<TreasureChest>();
                            break;
                        }
                        case 'X': arenaTrigger = feet; hasArena = true; break;
                        case 'Z': arenaDoor = new Vector2(x, y); hasDoor = true; break;
                    }
                }

            if (!castle) PlaceTrees(L, rng, Deco);

            // --- Player, camera, lighting
            player = (GameObject)PrefabUtility.InstantiatePrefab(ContentBuilder.LoadPrefab("Player"), scene);
            player.transform.position = spawn;
            var spawnPoint = new GameObject("SpawnPoint").transform;
            spawnPoint.position = spawn;

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6.75f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = castle ? new Color(0.07f, 0.05f, 0.12f) : new Color(0.16f, 0.22f, 0.4f);
            camGo.transform.position = new Vector3(spawn.x, spawn.y + 1.5f, -10f);
            camGo.AddComponent<AudioListener>();
            var cc = camGo.AddComponent<CameraController>();
            cc.target = player.transform;
            cc.bounds = new Rect(0f, 0f, L.w, L.h);

            var globalLight = new GameObject("Global Light 2D").AddComponent<Light2D>();
            globalLight.lightType = Light2D.LightType.Global;
            globalLight.intensity = spec.boss ? 0.7f : castle ? 0.72f : 0.95f;
            globalLight.color = castle ? new Color(0.68f, 0.62f, 0.95f) : new Color(1f, 0.97f, 0.92f);
            globalLight.targetSortingLayers = SortingLayer.layers.Select(l => l.id).ToArray();

            BuildParallax(L, castle, cam);

            // --- Kill zone below the level
            var kz = new GameObject("KillZone");
            kz.transform.position = new Vector3(L.w * 0.5f, -3f, 0f);
            var kzc = kz.AddComponent<BoxCollider2D>();
            kzc.isTrigger = true; kzc.size = new Vector2(L.w + 20f, 2f);
            kz.AddComponent<KillZone>();

            // --- Managers and UI
            var lmGo = new GameObject("LevelManager");
            var lm = lmGo.AddComponent<LevelManager>();
            lm.levelTitle = spec.title; lm.levelSubtitle = spec.subtitle; lm.nextScene = spec.nextScene;
            lm.player = player.GetComponent<PlayerController>();
            lm.spawnPoint = spawnPoint;
            lm.spriteMaterial = ContentBuilder.Unlit;
            lm.music = AssetDatabase.LoadAssetAtPath<AudioClip>($"{ContentBuilder.Root}/Audio/Music/{spec.music}");
            PrefabUtility.InstantiatePrefab(ContentBuilder.LoadPrefab("GameCanvas"), scene);
            UIBuilder.EventSystemObject();

            // --- Boss arena wiring
            if (spec.boss && boss != null && hasArena)
            {
                boss.treasure = finalChest;
                boss.arenaLeft = arenaTrigger.x - 1.5f;
                boss.arenaRight = L.w - 1f;
                boss.floorY = arenaTrigger.y;
                boss.ceilingY = L.h - 1f;
                var arena = new GameObject("BossArena") { layer = Layers.Interactables };
                arena.transform.position = arenaTrigger + new Vector2(0f, 2f);
                var ac = arena.AddComponent<BoxCollider2D>(); ac.isTrigger = true; ac.size = new Vector2(1f, 4f);
                var ba = arena.AddComponent<BossArena>();
                ba.boss = boss;
                if (hasDoor)
                {
                    var door = new GameObject("ArenaDoor") { layer = Layers.Ground };
                    door.transform.position = arenaDoor;
                    var dc = door.AddComponent<BoxCollider2D>(); dc.size = new Vector2(1f, 3f); dc.offset = new Vector2(0.5f, 1.5f);
                    var dv = new GameObject("Visual");
                    dv.transform.SetParent(door.transform, false);
                    var dsr = ContentBuilder.AddSprite(dv, C("FenceHalf"), "Platforms", -1);
                    dsr.drawMode = SpriteDrawMode.Tiled;
                    dsr.size = new Vector2(1f, 3f);
                    // Tiled sprites are centred on their pivot; offset so the bars fill the doorway.
                    dsr.transform.localPosition = Vector3.zero;
                    ba.doorCollider = dc; ba.doorVisual = dv.transform; ba.doorClosedY = 0f; ba.doorOpenY = 3f;
                }
            }

            ContentBuilder.EnsureFolder(Scenes);
            EditorSceneManager.SaveScene(scene, $"{Scenes}/{spec.scene}.unity");
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Floating pickups bob; place them so the lowest point of the bob stays a little above the cell's floor.
        /// </summary>
        static Vector2 PickupPosition(string prefab, int x, int y)
        {
            var go = ContentBuilder.LoadPrefab(prefab);
            var sr = go.GetComponentInChildren<SpriteRenderer>();
            var bob = go.GetComponentInChildren<Bobbing>();
            float bobAmp = bob != null ? Mathf.Abs(bob.amplitude.y) : 0f;
            float bottom = sr.sprite.bounds.min.y * sr.transform.lossyScale.y + sr.transform.localPosition.y;
            const float clearance = 0.08f;
            return new Vector2(x + 0.5f, y + clearance + bobAmp - bottom);
        }

        static bool HasRoof(Layout L, int x, int y)
        {
            for (int yy = y + 1; yy < L.h; yy++) if (L.cells[x, yy] == '#') return true;
            return false;
        }

        static Sprite GroundSprite(Layout L, int x, int y, bool castle, System.Random rng)
        {
            bool airAbove = !L.Solid(x, y + 1), airBelow = y > 0 && !L.Solid(x, y - 1);
            bool airLeft = !L.Solid(x - 1, y), airRight = !L.Solid(x + 1, y);
            if (castle)
            {
                if (airAbove) return C("Top_" + (x & 1));
                return C("Fill_" + ((x & 1) + 2 * (y & 1)));
            }
            if (airAbove)
                return F(airLeft && airRight ? "Top_Single" : airLeft ? "Top_Left" : airRight ? "Top_Right" : "Top_Mid");
            if (airBelow) return F(airLeft ? "Under_Left" : airRight ? "Under_Right" : "Under_Mid");
            if (airLeft) return F("FillL_" + (y & 1));
            if (airRight) return F("FillR_" + (y & 1));
            return F("Fill_" + rng.Next(5));
        }

        static Sprite OneWaySprite(Layout L, int x, int y, bool castle)
        {
            bool left = L.OneWay(x - 1, y), right = L.OneWay(x + 1, y);
            if (castle) return C("Plank_" + (!left ? 0 : !right ? 5 : 1 + (x % 4)));
            return F(!left ? "Ledge_Left" : !right ? "Ledge_Right" : (x & 1) == 0 ? "Ledge_Mid0" : "Ledge_Mid1");
        }

        static void PlaceTrees(Layout L, System.Random rng, System.Func<string, Sprite, Vector2, int, Color, string, GameObject> deco)
        {
            var trunk = ArtSetup.GetSprite("Environment/Forest/Forest_tree.png", "Tree_Trunk");
            var clump = ArtSetup.GetSprite("Environment/Forest/Forest_leaves.png", "LeafClump");
            for (int x = 6; x < L.w - 6; x += 9 + rng.Next(6))
            {
                int surface = -1;
                for (int y = L.h - 2; y >= 0; y--)
                    if (L.Solid(x, y) && !L.Solid(x, y + 1)) { surface = y + 1; break; }
                if (surface < 0) continue;
                bool open = true;
                for (int yy = surface; yy < Mathf.Min(L.h, surface + 9); yy++)
                    for (int xx = x - 2; xx <= x + 2; xx++)
                        if (L.At(xx, yy) != '.' && L.At(xx, yy) != ',' && L.At(xx, yy) != '*') open = false;
                if (!open) continue;
                var tint = new Color(0.55f, 0.6f, 0.6f);
                deco("TreeTrunk", trunk, new Vector2(x + 0.5f, surface - 0.2f), 20, tint, "Background");
                foreach (var o in new[] { new Vector2(-1.4f, 4.4f), new Vector2(1.6f, 4.6f), new Vector2(0.1f, 5.6f), new Vector2(-0.2f, 4.1f) })
                    deco("TreeLeaves", clump, new Vector2(x + 0.5f + o.x, surface + o.y), 21, tint, "Background");
            }
        }

        static void BuildParallax(Layout L, bool castle, Camera cam)
        {
            var root = new GameObject("Parallax");
            float cx = cam.transform.position.x, cy = cam.transform.position.y;
            GameObject Layer(string name, Sprite s, Vector2 factor, float y, int order, Color tint, bool tiled, float xOffset = 0f)
            {
                var go = new GameObject(name);
                go.transform.SetParent(root.transform, false);
                var sr = ContentBuilder.AddSprite(go, s, "Background", order);
                sr.color = tint;
                if (tiled)
                {
                    sr.drawMode = SpriteDrawMode.Tiled;
                    sr.size = new Vector2((1f - factor.x) * L.w * 2f + 60f, s.bounds.size.y);
                }
                go.transform.position = new Vector3(cx + xOffset, y, 0f);
                go.AddComponent<ParallaxLayer>().factor = factor;
                return go;
            }
            Sprite Bg(string n) => ArtSetup.SingleSprite($"Environment/Forest/Forest_{n}.png");

            if (castle)
            {
                float horizon = Mathf.Max(L.backwall, 0) + 1f;
                Layer("Stars", Bg("stars"), new Vector2(0.97f, 0.97f), cy - 4f, -100, new Color(0.8f, 0.7f, 1f), true);
                Layer("Moon", Bg("moon"), new Vector2(0.98f, 0.98f), cy + 2f, -95, new Color(0.9f, 0.85f, 1f), false, 7f);
                Layer("FarHills", Bg("mountain_dark"), new Vector2(0.85f, 0.85f), horizon - 1f, -90, new Color(0.3f, 0.22f, 0.45f), true);
            }
            else
            {
                Layer("Stars", Bg("stars"), new Vector2(0.97f, 0.97f), cy - 2f, -100, new Color(0.7f, 0.75f, 1f, 0.6f), true);
                var cloud = ArtSetup.GetSprite("Environment/Forest/Forest_clouds.png", "Cloud_Light");
                for (int i = 0; i < 6; i++)
                    Layer("Cloud" + i, cloud, new Vector2(0.9f, 0.92f), cy + 3f + (i % 3) * 1.2f, -92, new Color(0.75f, 0.8f, 0.95f, 0.8f), false, -10f + i * 9f);
                Layer("MountainsFar", Bg("mountain_large_blue"), new Vector2(0.85f, 0.88f), 1f, -90, new Color(0.62f, 0.68f, 0.88f), true);
                Layer("MountainsNear", Bg("mountain_blue"), new Vector2(0.7f, 0.78f), 1.5f, -85, new Color(0.42f, 0.55f, 0.62f), true);
            }
        }
    }
}
