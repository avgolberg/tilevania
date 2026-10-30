using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace TileVania.EditorTools
{
    /// <summary>
    /// Configures every imported texture consistently (point filter, no compression, no mips,
    /// explicit pixels-per-unit) and slices sprite sheets with stable sprite IDs.
    /// Pixel rectangles are given in image coordinates (origin top-left) and converted here.
    /// </summary>
    public static class ArtSetup
    {
        public const string Art = "Assets/_Game/Art";

        public struct Slice
        {
            public string name;
            public int x, y, w, h;  // top-left origin
            public Vector2 pivot;

            public Slice(string name, int x, int y, int w, int h, Vector2 pivot)
            {
                this.name = name; this.x = x; this.y = y; this.w = w; this.h = h; this.pivot = pivot;
            }
        }

        static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);
        static readonly Vector2 BottomLeft = Vector2.zero;

        public static void ConfigureAll()
        {
            GeneratedArt.CreateAll();

            // Player (STAEL Adventurer Hero) — 16 PPU so a 16px tile is one unit
            var heroPivot = new Vector2(15f / 32f, 0f);
            Strip("Player/Hero_Idle.png", "Hero_Idle", 32, 32, 4, 16, heroPivot);
            Strip("Player/Hero_Run.png", "Hero_Run", 32, 32, 8, 16, heroPivot);
            Strip("Player/Hero_Jump.png", "Hero_Jump", 32, 32, 4, 16, heroPivot);
            Strip("Player/Hero_Fall.png", "Hero_Fall", 32, 32, 4, 16, heroPivot);
            Strip("Player/Hero_Apex.png", "Hero_Apex", 32, 32, 3, 16, heroPivot);
            Strip("Player/Hero_Attack.png", "Hero_Attack", 48, 32, 6, 16, new Vector2(30f / 48f, 0f));
            Sheet("FX/Hero_SlashFX.png", 24, new[] { new Slice("Slash", 3, 7, 30, 24, Center) });

            // Enemies (higher-resolution sheets, 80 PPU keeps them close to the hero's size)
            Sheet("Enemies/Enemy_skeleton.png", 80, Enumerable.Range(0, 4)
                .Select(i => new Slice("Skeleton_" + i, new[] { 23, 161, 299, 437 }[i], 12, 84, 122, BottomCenter)).ToArray());
            Strip("Enemies/Enemy_slime.png", "Slime", 74, 86, 4, 80, new Vector2(0.5f, 5f / 86f));
            Strip("Enemies/Enemy_bat.png", "Bat", 127, 138, 3, 80, Center);
            Strip("Enemies/Enemy_ghost.png", "Ghost", 75, 138, 2, 80, Center);
            Strip("Enemies/Enemy_spider.png", "Spider", 138, 138, 6, 80, Center);

            // Forest (Glax pixel art platformer pack)
            var forest = new List<Slice>();
            void FCell(string n, int cx, int cy) => forest.Add(new Slice("Forest_" + n, cx * 16, cy * 16, 16, 16, Center));
            FCell("Top_Mid", 2, 0); FCell("Top_Left", 1, 0); FCell("Top_Right", 3, 0); FCell("Top_Single", 8, 0);
            FCell("Fill_0", 6, 2); FCell("Fill_1", 7, 2); FCell("Fill_2", 5, 3); FCell("Fill_3", 6, 3); FCell("Fill_4", 7, 3);
            FCell("FillL_0", 10, 2); FCell("FillL_1", 10, 3); FCell("FillR_0", 11, 2); FCell("FillR_1", 11, 3);
            FCell("Under_Mid", 2, 2); FCell("Under_Left", 1, 2); FCell("Under_Right", 3, 2);
            FCell("Ledge_Left", 4, 0); FCell("Ledge_Mid0", 5, 0); FCell("Ledge_Mid1", 6, 0); FCell("Ledge_Right", 7, 0);
            FCell("Grass_0", 5, 1); FCell("Grass_1", 6, 1); FCell("Flower_0", 7, 1); FCell("Flower_1", 8, 1);
            forest.Add(new Slice("Forest_Spike_0", 0, 48, 16, 16, BottomCenter));
            forest.Add(new Slice("Forest_Spike_1", 16, 48, 16, 16, BottomCenter));
            forest.Add(new Slice("Forest_Spike_2", 32, 48, 16, 16, BottomCenter));
            forest.Add(new Slice("Forest_ColumnShort", 48, 48, 16, 16, BottomCenter));
            forest.Add(new Slice("Forest_Column", 64, 56, 16, 24, BottomCenter));
            forest.Add(new Slice("Forest_Fire", 96, 64, 16, 16, BottomCenter));
            forest.Add(new Slice("Forest_FireBall", 96, 64, 16, 16, new Vector2(0.5f, 9.5f / 16f)));
            Sheet("Environment/Forest/Forest_Tiles.png", 16, forest.ToArray());

            Sheet("Environment/Forest/Forest_ruins.png", 16, new[] { new Slice("Ruins_Arch", 0, 64, 64, 64, BottomLeft) });
            Sheet("Environment/Forest/Forest_tree.png", 16, new[] { new Slice("Tree_Trunk", 128, 0, 80, 80, new Vector2(0.42f, 0f)) });
            Sheet("Environment/Forest/Forest_leaves.png", 16, new[]
            {
                new Slice("Bush_Big", 0, 0, 64, 32, BottomCenter),
                new Slice("Bush_Small", 88, 0, 32, 32, BottomCenter),
                new Slice("Canopy", 112, 96, 80, 112, BottomCenter),
                new Slice("LeafClump", 0, 96, 40, 40, Center),
            });
            Sheet("Environment/Forest/Forest_clouds.png", 16, new[]
            {
                new Slice("Cloud_Light", 0, 0, 48, 48, Center),
                new Slice("Cloud_Mid", 0, 64, 48, 48, Center),
                new Slice("Cloud_Dark", 0, 128, 48, 48, Center),
            });
            foreach (var bg in new[] { "stars", "starfield", "moon", "mountain_large_blue", "mountain_blue", "mountain_dark" })
                Single($"Environment/Forest/Forest_{bg}.png", 16, BottomCenter, fullRect: true);
            Single("Environment/Forest/Forest_signs.png", 16, BottomCenter);
            Single("Environment/Forest/Forest_rock.png", 16, Center);

            // Castle (Spooky Castle tileset)
            var castle = new List<Slice>();
            void CCell(string n, int cx, int cy) => castle.Add(new Slice("Castle_" + n, cx * 16, cy * 16, 16, 16, Center));
            CCell("Cap_0", 3, 1); CCell("Cap_1", 4, 1);
            CCell("Top_0", 3, 2); CCell("Top_1", 4, 2);
            CCell("Fill_0", 3, 3); CCell("Fill_1", 4, 3); CCell("Fill_2", 3, 4); CCell("Fill_3", 4, 4);
            for (int i = 0; i < 4; i++) { CCell("BackTop_" + i, 5 + i, 12); CCell("BackFill_" + i, 5 + i, 13); CCell("BackFill2_" + i, 5 + i, 14); }
            for (int i = 0; i < 6; i++) castle.Add(new Slice("Castle_Plank_" + i, 288 + i * 16, 92, 16, 16, Center));
            castle.Add(new Slice("Castle_Torch", 232, 134, 16, 20, Center));
            castle.Add(new Slice("Castle_WindowRound", 288, 192, 32, 32, Center));
            castle.Add(new Slice("Castle_WindowNarrow", 320, 192, 32, 32, Center));
            castle.Add(new Slice("Castle_DoorIron", 288, 224, 32, 32, BottomCenter));
            castle.Add(new Slice("Castle_DoorWood", 352, 224, 32, 32, BottomCenter));
            castle.Add(new Slice("Castle_Fence", 288, 288, 32, 32, BottomLeft));
            castle.Add(new Slice("Castle_FenceHalf", 288, 288, 16, 32, BottomLeft));
            castle.Add(new Slice("Castle_Crate", 320, 288, 32, 32, BottomCenter));
            castle.Add(new Slice("Castle_Barrel", 352, 288, 32, 32, BottomCenter));
            Sheet("Environment/Castle/Castle_Tiles.png", 16, castle.ToArray(), fullRect: true);

            // Items
            Sheet("Items/Gems_CaveTileset.png", 16, new[]
            {
                new Slice("Gem_Red", 0, 160, 16, 16, Center), new Slice("Gem_Green", 16, 160, 16, 16, Center),
                new Slice("Gem_Blue", 32, 160, 16, 16, Center), new Slice("Gem_Orange", 48, 160, 16, 16, Center),
            });
            foreach (var k in new[] { "03", "05", "02" }) Single($"Items/Key_{k}.png", 28, Center);
            foreach (var c in new[] { "Iron", "Silver", "Gold" })
                Strip($"Items/Chest_{c}.png", "Chest_" + c, 48, 48, 3, 16, new Vector2(18.5f / 48f, 12f / 48f));

            // Portals
            Strip("Portals/Portal_Loop.png", "PortalLoop", 32, 32, 5, 16, BottomCenter);
            var open = new List<Slice>();
            for (int i = 0; i < 17; i++) open.Add(new Slice("PortalOpen_" + i.ToString("00"), (i % 4) * 32, (i / 4) * 32, 32, 32, BottomCenter));
            Sheet("Portals/Portal_Open.png", 16, open.ToArray());

            // Boss (Glax dragon, upscaled 2x via 8 PPU)
            Single("Boss/Dragon_A.png", 8, BottomCenter);
            Single("Boss/Dragon_B.png", 8, BottomCenter);

            AssetDatabase.SaveAssets();
        }

        static TextureImporter Prepare(string relPath, int ppu, SpriteImportMode mode, bool fullRect)
        {
            string path = $"{Art}/{relPath}";
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            if (imp == null) throw new FileNotFoundException("Missing texture " + path);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = mode;
            imp.spritePixelsPerUnit = ppu;
            imp.filterMode = FilterMode.Point;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            var s = new TextureImporterSettings();
            imp.ReadTextureSettings(s);
            s.spriteMeshType = fullRect ? SpriteMeshType.FullRect : SpriteMeshType.Tight;
            s.spriteExtrude = 0;
            s.spriteGenerateFallbackPhysicsShape = false;
            imp.SetTextureSettings(s);
            imp.SaveAndReimport();
            return imp;
        }

        public static void Single(string relPath, int ppu, Vector2 pivot, bool fullRect = false)
        {
            var imp = Prepare(relPath, ppu, SpriteImportMode.Single, fullRect);
            var s = new TextureImporterSettings();
            imp.ReadTextureSettings(s);
            s.spriteAlignment = (int)SpriteAlignment.Custom;
            s.spritePivot = pivot;
            imp.SetTextureSettings(s);
            imp.SaveAndReimport();
        }

        public static void Strip(string relPath, string baseName, int fw, int fh, int count, int ppu, Vector2 pivot)
        {
            var slices = new Slice[count];
            for (int i = 0; i < count; i++) slices[i] = new Slice($"{baseName}_{i}", i * fw, 0, fw, fh, pivot);
            Sheet(relPath, ppu, slices);
        }

        public static void Sheet(string relPath, int ppu, Slice[] slices, bool fullRect = false)
        {
            var imp = Prepare(relPath, ppu, SpriteImportMode.Multiple, fullRect);
            imp.GetSourceTextureWidthAndHeight(out int texW, out int texH);

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var dp = factory.GetSpriteEditorDataProviderFromObject(imp);
            dp.InitSpriteEditorDataProvider();

            // Keep sprite IDs stable across rebuilds so existing references stay valid.
            var existing = dp.GetSpriteRects().ToDictionary(r => r.name, r => r.spriteID);
            var rects = new List<SpriteRect>();
            foreach (var sl in slices)
            {
                var r = new SpriteRect
                {
                    name = sl.name,
                    rect = new Rect(sl.x, texH - sl.y - sl.h, sl.w, sl.h),
                    alignment = SpriteAlignment.Custom,
                    pivot = sl.pivot,
                    border = Vector4.zero,
                    spriteID = existing.TryGetValue(sl.name, out var id) ? id : GUID.Generate(),
                };
                rects.Add(r);
            }
            dp.SetSpriteRects(rects.ToArray());
            var names = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
            names?.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            dp.Apply();
            imp.SaveAndReimport();
        }

        public static Sprite GetSprite(string relPath, string name)
        {
            var all = AssetDatabase.LoadAllAssetsAtPath($"{Art}/{relPath}");
            var s = all.OfType<Sprite>().FirstOrDefault(x => x.name == name);
            if (s == null) throw new System.Exception($"Sprite '{name}' not found in {relPath}");
            return s;
        }

        public static Sprite SingleSprite(string relPath)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>($"{Art}/{relPath}");
            if (s == null) throw new System.Exception($"Sprite not found at {relPath}");
            return s;
        }

        public static Sprite[] Frames(string relPath, string baseName, int count)
        {
            var arr = new Sprite[count];
            for (int i = 0; i < count; i++) arr[i] = GetSprite(relPath, $"{baseName}_{i}");
            return arr;
        }
    }
}
