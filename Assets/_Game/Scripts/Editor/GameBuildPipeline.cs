using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace TileVania.EditorTools
{
    /// <summary>
    /// One-click (or batch-mode) regeneration of the whole game:
    /// Tools > TileVania > Rebuild Game, or
    /// Unity.exe -batchmode -projectPath ... -executeMethod TileVania.EditorTools.GameBuildPipeline.BuildAllBatch -quit
    /// </summary>
    public static class GameBuildPipeline
    {
        public static readonly LevelBuilder.LevelSpec[] Levels =
        {
            new LevelBuilder.LevelSpec
            {
                layout = "Level1_Forest", scene = SceneLoader.Level1, title = "The Whispering Forest",
                subtitle = "Find the way into the ancient ruins", nextScene = SceneLoader.Level2,
                music = "Music_PerilousDungeon.mp3",
            },
            new LevelBuilder.LevelSpec
            {
                layout = "Level2_Castle", scene = SceneLoader.Level2, title = "The Abandoned Castle",
                subtitle = "Two keys guard the treasure chamber", nextScene = SceneLoader.Boss,
                music = "Music_SpookyDungeon.ogg", castle = true,
            },
            new LevelBuilder.LevelSpec
            {
                layout = "Level3_TreasureChamber", scene = SceneLoader.Boss, title = "The Treasure Chamber",
                subtitle = "Something ancient stirs...", nextScene = SceneLoader.MainMenu,
                music = "Music_SpookyDungeon.ogg", castle = true, boss = true,
            },
        };

        [MenuItem("Tools/TileVania/Rebuild Game (art, prefabs, scenes)")]
        public static void BuildAll()
        {
            ContentBuilder.SetupProject();
            ArtSetup.ConfigureAll();
            ContentBuilder.BuildSfxLibrary();
            ContentBuilder.BuildPrefabs();
            UIBuilder.BuildGameCanvasPrefab();
            BuildMainMenu();
            foreach (var l in Levels) LevelBuilder.Build(l);
            SetBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("[TileVania] Build complete.\n" + Validate());
        }

        public static void BuildAllBatch()
        {
            try
            {
                BuildAll();
                CaptureScreenshots();
                EditorApplication.Exit(0);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>Builds a Windows player into Builds/Windows (git-ignored) to verify the game builds.</summary>
        [MenuItem("Tools/TileVania/Build Windows Player")]
        public static void BuildWindowsPlayer()
        {
            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = "Builds/Windows/TileVania.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"[TileVania] Player build: {report.summary.result}, errors={report.summary.totalErrors}, size={report.summary.totalSize / (1024 * 1024)} MB");
        }

        public static void BuildWindowsPlayerBatch()
        {
            BuildWindowsPlayer();
            EditorApplication.Exit(0);
        }

        public static void ValidateBatch()
        {
            Debug.Log("[TileVania] " + Validate());
            EditorApplication.Exit(0);
        }

        static void SetBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene($"{LevelBuilder.Scenes}/{SceneLoader.MainMenu}.unity", true),
            };
            scenes.AddRange(Levels.Select(l => new EditorBuildSettingsScene($"{LevelBuilder.Scenes}/{l.scene}.unity", true)));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ------------------------------------------------------------------ main menu
        static void BuildMainMenu()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6.75f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.16f, 0.22f, 0.4f);
            camGo.transform.position = new Vector3(0f, 6.75f, -10f);
            camGo.AddComponent<AudioListener>();

            var light = new GameObject("Global Light 2D").AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 0.9f;
            light.targetSortingLayers = SortingLayer.layers.Select(l => l.id).ToArray();

            Sprite F(string n) => ArtSetup.GetSprite("Environment/Forest/Forest_Tiles.png", "Forest_" + n);
            Sprite Bg(string n) => ArtSetup.SingleSprite($"Environment/Forest/Forest_{n}.png");
            GameObject MakeSprite(string name, Sprite s, Vector2 pos, string layer, int order, Color tint, bool unlit = false)
            {
                var go = new GameObject(name);
                go.transform.position = pos;
                ContentBuilder.AddSprite(go, s, layer, order, unlit).color = tint;
                return go;
            }

            var stars = MakeSprite("Stars", Bg("stars"), new Vector2(0, 4), "Background", -100, new Color(0.7f, 0.75f, 1f, 0.6f));
            stars.GetComponent<SpriteRenderer>().drawMode = SpriteDrawMode.Tiled;
            stars.GetComponent<SpriteRenderer>().size = new Vector2(40, 16);
            var far = MakeSprite("Mountains", Bg("mountain_large_blue"), new Vector2(0, 0.5f), "Background", -90, new Color(0.62f, 0.68f, 0.88f));
            far.GetComponent<SpriteRenderer>().drawMode = SpriteDrawMode.Tiled;
            far.GetComponent<SpriteRenderer>().size = new Vector2(40, 12);
            var near = MakeSprite("MountainsNear", Bg("mountain_blue"), new Vector2(0, 1f), "Background", -85, new Color(0.42f, 0.55f, 0.62f));
            near.GetComponent<SpriteRenderer>().drawMode = SpriteDrawMode.Tiled;
            near.GetComponent<SpriteRenderer>().size = new Vector2(40, 8);

            var grid = new GameObject("Grid").AddComponent<Grid>();
            var tmGo = new GameObject("Ground");
            tmGo.transform.SetParent(grid.transform, false);
            var tm = tmGo.AddComponent<Tilemap>();
            var tr = tmGo.AddComponent<TilemapRenderer>();
            tr.sortingLayerName = "Platforms";
            tr.sharedMaterial = ContentBuilder.Lit;
            var rng = new System.Random(7);
            for (int x = -16; x < 16; x++)
                for (int y = 0; y < 2; y++)
                {
                    tm.SetTile(new Vector3Int(x, y, 0), LevelBuilder.TileFor(y == 1 ? F("Top_Mid") : F("Fill_" + rng.Next(5))));
                }

            MakeSprite("TreeTrunk", ArtSetup.GetSprite("Environment/Forest/Forest_tree.png", "Tree_Trunk"), new Vector2(-9f, 1.8f), "Background", 20, new Color(0.55f, 0.6f, 0.6f));
            var clump = ArtSetup.GetSprite("Environment/Forest/Forest_leaves.png", "LeafClump");
            foreach (var o in new[] { new Vector2(-10.6f, 6.2f), new Vector2(-7.6f, 6.4f), new Vector2(-9.1f, 7.6f), new Vector2(-9.2f, 5.9f) })
                MakeSprite("TreeLeaves", clump, o, "Background", 21, new Color(0.55f, 0.6f, 0.6f));
            var column = ArtSetup.GetSprite("Environment/Forest/Forest_Tiles.png", "Forest_Column");
            MakeSprite("Column", column, new Vector2(3f, 2f), "Background", 30, new Color(0.8f, 0.8f, 0.85f)).transform.localScale = new Vector3(2f, 2f, 1f);
            var hero = MakeSprite("Hero", ArtSetup.GetSprite("Player/Hero_Idle.png", "Hero_Idle_0"), new Vector2(-4f, 2f), "Player", 0, Color.white);
            var heroAnim = hero.AddComponent<SpriteFrameAnimator>();
            heroAnim.frames = ArtSetup.Frames("Player/Hero_Idle.png", "Hero_Idle", 4); heroAnim.fps = 8f;
            hero.transform.localScale = new Vector3(2f, 2f, 1f);
            var chest = MakeSprite("Chest", ArtSetup.GetSprite("Items/Chest_Gold.png", "Chest_Gold_2"), new Vector2(7f, 2f), "Interactables", 0, Color.white);
            chest.transform.localScale = new Vector3(2f, 2f, 1f);
            ContentBuilder.AddPointLight(new GameObject("ChestGlow"), new Color(1f, 0.8f, 0.4f), 5f, 1f).transform.position = new Vector3(7f, 3f, 0f);
            var portal = MakeSprite("Portal", ArtSetup.GetSprite("Portals/Portal_Loop.png", "PortalLoop_0"), new Vector2(10.5f, 2f), "Interactables", 0, Color.white, true);
            portal.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
            var pa = portal.AddComponent<SpriteFrameAnimator>();
            pa.frames = ArtSetup.Frames("Portals/Portal_Loop.png", "PortalLoop", 5); pa.fps = 8f;

            var ctrlGo = new GameObject("MainMenu");
            var ctrl = ctrlGo.AddComponent<MainMenuController>();
            ctrl.music = AssetDatabase.LoadAssetAtPath<AudioClip>(ContentBuilder.Root + "/Audio/Music/Music_PerilousDungeon.mp3");
            UIBuilder.BuildMainMenuCanvas(ctrl);
            UIBuilder.EventSystemObject();

            ContentBuilder.EnsureFolder(LevelBuilder.Scenes);
            EditorSceneManager.SaveScene(scene, $"{LevelBuilder.Scenes}/{SceneLoader.MainMenu}.unity");
        }

        // ------------------------------------------------------------------ validation
        [MenuItem("Tools/TileVania/Validate Scenes")]
        public static string Validate()
        {
            var sb = new StringBuilder();
            int problems = 0;
            foreach (var s in EditorBuildSettings.scenes)
            {
                if (!File.Exists(s.path)) { sb.AppendLine($"MISSING SCENE FILE {s.path}"); problems++; continue; }
                var scene = EditorSceneManager.OpenScene(s.path, OpenSceneMode.Single);
                int missingScripts = 0, missingRefs = 0;
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    {
                        missingScripts += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                        foreach (var c in t.GetComponents<Component>())
                        {
                            if (c == null) continue;
                            var so = new SerializedObject(c);
                            var it = so.GetIterator();
                            while (it.NextVisible(true))
                                if (it.propertyType == SerializedPropertyType.ObjectReference && it.objectReferenceValue == null
                                    && it.objectReferenceInstanceIDValue != 0)
                                {
                                    missingRefs++;
                                    sb.AppendLine($"  missing ref: {t.name}.{c.GetType().Name}.{it.propertyPath}");
                                }
                        }
                    }
                var sprites = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Count(r => r.sprite == null && r.GetComponent<SpriteFrameAnimator>() == null && r.drawMode == SpriteDrawMode.Simple);
                bool hasManager = Object.FindFirstObjectByType<LevelManager>() != null || Object.FindFirstObjectByType<MainMenuController>() != null;
                bool hasEvents = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null;
                sb.AppendLine($"{s.path}: missingScripts={missingScripts} missingRefs={missingRefs} emptySprites={sprites} manager={hasManager} eventSystem={hasEvents}");
                problems += missingScripts + missingRefs + (hasManager ? 0 : 1) + (hasEvents ? 0 : 1);

                foreach (var comp in Object.FindObjectsByType<CompositeCollider2D>(FindObjectsSortMode.None))
                    if (comp.pathCount == 0) { sb.AppendLine($"  empty collider geometry on {comp.name}"); problems++; }

                var lm = Object.FindFirstObjectByType<LevelManager>();
                if (lm != null)
                {
                    bool nextOk = EditorBuildSettings.scenes.Any(b => Path.GetFileNameWithoutExtension(b.path) == lm.nextScene);
                    if (!nextOk) { sb.AppendLine($"  nextScene '{lm.nextScene}' not in build settings"); problems++; }
                    if (lm.player == null || lm.music == null) { sb.AppendLine("  LevelManager missing player or music"); problems++; }
                }
            }
            sb.Insert(0, $"Validation: {problems} problem(s)\n");
            File.WriteAllText("Temp/tilevania_validation.txt", sb.ToString());
            return sb.ToString();
        }

        // ------------------------------------------------------------------ screenshots
        [MenuItem("Tools/TileVania/Capture Preview Screenshots")]
        public static void CaptureScreenshots()
        {
            Directory.CreateDirectory("Docs/Screenshots");
            Capture(SceneLoader.MainMenu, "", new[] { new Vector2(0f, 6.75f) });
            Capture(SceneLoader.MainMenu, "_credits", new[] { new Vector2(0f, 6.75f) }, () => ShowOnly("CreditsPanel", "MainPanel"));
            Capture(SceneLoader.Level1, "", new[] { new Vector2(12, 12), new Vector2(48, 14), new Vector2(96, 6.75f), new Vector2(116, 6.75f) }, ShowHud);
            Capture(SceneLoader.Level1, "_pause", new[] { new Vector2(12, 12) }, () => ShowOnly("PausePanel"));
            Capture(SceneLoader.Level2, "", new[] { new Vector2(12, 6.75f), new Vector2(50, 8), new Vector2(68, 15), new Vector2(92, 8), new Vector2(128, 8) }, ShowHud);
            Capture(SceneLoader.Boss, "", new[] { new Vector2(12, 7), new Vector2(26, 10) }, () =>
            {
                ShowHud();
                Find("BossBar").SetActive(true);
                var boss = Object.FindFirstObjectByType<DragonBoss>(FindObjectsInactive.Include);
                boss.transform.position = new Vector3(28f, 12f, 0f);
            });
            Capture(SceneLoader.Boss, "_victory", new[] { new Vector2(26, 10) }, () => ShowOnly("VictoryPanel"));
        }

        static GameObject Find(string name) =>
            Resources.FindObjectsOfTypeAll<GameObject>().First(g => g.name == name && g.scene.IsValid());

        static void ShowHud()
        {
            var hud = Object.FindFirstObjectByType<HUD>();
            hud.titleGroup.alpha = 0f;
            // Show the HUD as it looks during normal play: no toast, no keys held.
            hud.messageGroup.alpha = 0f;
            hud.goldKeyIcon.SetActive(false);
            hud.treasureKeyIcon.SetActive(false);
            hud.gemText.text = "12";
        }

        static void ShowOnly(string show, string hide = null)
        {
            Find(show).SetActive(true);
            if (hide != null) Find(hide).SetActive(false);
        }

        static void Capture(string sceneName, string suffix, Vector2[] positions, System.Action setup = null)
        {
            EditorSceneManager.OpenScene($"{LevelBuilder.Scenes}/{sceneName}.unity", OpenSceneMode.Single);
            var cam = Camera.main;
            setup?.Invoke();
            // Overlay canvases don't render into a camera; switch them for the capture only (scene is not saved).
            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = cam;
                c.planeDistance = 1f;
                c.sortingLayerName = "Foreground";
                c.sortingOrder = 1000;
            }
            var rt = new RenderTexture(1920, 1080, 24);
            var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            for (int i = 0; i < positions.Length; i++)
            {
                cam.transform.position = new Vector3(positions[i].x, positions[i].y, -10f);
                cam.targetTexture = rt;
                Canvas.ForceUpdateCanvases();
                cam.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                tex.Apply();
                File.WriteAllBytes($"Docs/Screenshots/{sceneName}{suffix}_{i}.png", tex.EncodeToPNG());
            }
            cam.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
        }
    }
}
