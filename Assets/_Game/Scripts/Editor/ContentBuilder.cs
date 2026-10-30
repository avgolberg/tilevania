using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace TileVania.EditorTools
{
    /// <summary>Project settings, materials, animation assets, gameplay prefabs and the SFX library.</summary>
    public static class ContentBuilder
    {
        public const string Root = "Assets/_Game";
        public const string Prefabs = Root + "/Prefabs";
        public const string Anims = Root + "/Animations";

        public static Material Lit => AssetDatabase.LoadAssetAtPath<Material>(
            "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat");
        public static Material Unlit => AssetDatabase.LoadAssetAtPath<Material>(
            "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");

        // ------------------------------------------------------------------ project settings
        public static void SetupProject()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            void SetLayer(int i, string n) => layers.GetArrayElementAtIndex(i).stringValue = n;
            SetLayer(Layers.Ground, "Ground"); SetLayer(Layers.Ladder, "Ladder"); SetLayer(Layers.Player, "Player");
            SetLayer(Layers.Background, "Background"); SetLayer(Layers.OneWay, "OneWay"); SetLayer(Layers.Enemy, "Enemy");
            SetLayer(Layers.Hazards, "Hazards"); SetLayer(Layers.PlayerProjectile, "PlayerProjectile");
            SetLayer(Layers.Interactables, "Interactables"); SetLayer(Layers.EnemyProjectile, "EnemyProjectile");

            var sorting = tagManager.FindProperty("m_SortingLayers");
            foreach (var name in new[] { "FX", "Foreground" })
            {
                bool exists = false;
                for (int i = 0; i < sorting.arraySize; i++)
                    if (sorting.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == name) exists = true;
                if (exists) continue;
                sorting.InsertArrayElementAtIndex(sorting.arraySize);
                var e = sorting.GetArrayElementAtIndex(sorting.arraySize - 1);
                e.FindPropertyRelative("name").stringValue = name;
                e.FindPropertyRelative("uniqueID").longValue = (uint)name.GetHashCode() & 0x7fffffff;
                e.FindPropertyRelative("locked").intValue = 0;
            }

            tagManager.ApplyModifiedPropertiesWithoutUndo();

            // Persist the 2D collision matrix (it is also enforced at runtime by Layers.ApplyCollisionMatrix)
            var physics = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/Physics2DSettings.asset")[0]);
            var matrix = physics.FindProperty("m_LayerCollisionMatrix");
            int[] game = { Layers.Ground, Layers.Ladder, Layers.Player, Layers.Background, Layers.OneWay, Layers.Enemy,
                Layers.Hazards, Layers.PlayerProjectile, Layers.Interactables, Layers.EnemyProjectile };
            for (int a = 0; a < 32; a++)
            {
                var p = matrix.GetArrayElementAtIndex(a);
                uint bits = p.uintValue;
                foreach (int b in game)
                {
                    if (!game.Contains(a)) continue;
                    if (Layers.ShouldCollide(a, b)) bits |= 1u << b; else bits &= ~(1u << b);
                }
                p.uintValue = bits;
            }
            physics.ApplyModifiedPropertiesWithoutUndo();
            Layers.ApplyCollisionMatrix();

            PlayerSettings.productName = "TileVania - Treasure Hunt";
            AssetDatabase.SaveAssets();
        }

        // ------------------------------------------------------------------ helpers
        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        public static SpriteRenderer AddSprite(GameObject go, Sprite s, string sortingLayer, int order = 0, bool unlit = false)
        {
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sharedMaterial = unlit ? Unlit : Lit;
            sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = order;
            return sr;
        }

        public static GameObject Child(GameObject parent, string name, Vector3 localPos = default)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPos;
            go.layer = parent.layer;
            return go;
        }

        public static Light2D AddPointLight(GameObject go, Color c, float radius, float intensity)
        {
            var l = go.AddComponent<Light2D>();
            l.lightType = Light2D.LightType.Point;
            l.color = c;
            l.intensity = intensity;
            l.pointLightOuterRadius = radius;
            l.pointLightInnerRadius = radius * 0.15f;
            l.falloffIntensity = 0.65f;
            l.shadowsEnabled = false;
            l.targetSortingLayers = SortingLayer.layers.Select(x => x.id).ToArray();
            return l;
        }

        static T Save<T>(GameObject go, string name) where T : Component
        {
            EnsureFolder(Prefabs);
            string path = $"{Prefabs}/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return typeof(T) == typeof(Transform) ? (T)(Component)asset.transform : asset.GetComponent<T>();
        }

        public static GameObject LoadPrefab(string name) => AssetDatabase.LoadAssetAtPath<GameObject>($"{Prefabs}/{name}.prefab");

        static Rigidbody2D Kinematic(GameObject go)
        {
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            return rb;
        }

        static SpriteFrameAnimator Animate(GameObject go, Sprite[] frames, float fps, bool loop = true)
        {
            var a = go.AddComponent<SpriteFrameAnimator>();
            a.frames = frames; a.fps = fps; a.loop = loop;
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null && frames.Length > 0) sr.sprite = frames[0]; // visible in the editor too
            return a;
        }

        // ------------------------------------------------------------------ animation
        static AnimationClip Clip(string name, Sprite[] frames, float fps, bool loop)
        {
            EnsureFolder(Anims);
            var clip = new AnimationClip { frameRate = fps };
            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            var keys = new ObjectReferenceKeyframe[frames.Length + 1];
            for (int i = 0; i < frames.Length; i++) keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = frames[i] };
            keys[frames.Length] = new ObjectReferenceKeyframe { time = frames.Length / fps, value = frames[frames.Length - 1] };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            string path = $"{Anims}/Hero_{name}.anim";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        static RuntimeAnimatorController BuildPlayerController()
        {
            const string p = "Player/";
            var idle = Clip("Idle", ArtSetup.Frames(p + "Hero_Idle.png", "Hero_Idle", 4), 8f, true);
            var run = Clip("Run", ArtSetup.Frames(p + "Hero_Run.png", "Hero_Run", 8), 14f, true);
            var jump = Clip("Jump", ArtSetup.Frames(p + "Hero_Jump.png", "Hero_Jump", 4), 14f, false);
            var apex = Clip("Apex", ArtSetup.Frames(p + "Hero_Apex.png", "Hero_Apex", 3), 12f, false);
            var fall = Clip("Fall", ArtSetup.Frames(p + "Hero_Fall.png", "Hero_Fall", 4), 10f, true);
            // The pack has no climb cycle; the jump frames loop slowly as a stand-in.
            var climb = Clip("Climb", ArtSetup.Frames(p + "Hero_Jump.png", "Hero_Jump", 4), 8f, true);
            var attack = Clip("Attack", ArtSetup.Frames(p + "Hero_Attack.png", "Hero_Attack", 6), 24f, false);
            var hurt = Clip("Hurt", new[] { ArtSetup.GetSprite(p + "Hero_Apex.png", "Hero_Apex_2") }, 10f, false);

            string path = $"{Anims}/Hero.controller";
            AssetDatabase.DeleteAsset(path);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
            var sm = ctrl.layers[0].stateMachine;
            foreach (var (n, c) in new[] { ("Idle", idle), ("Run", run), ("Jump", jump), ("Apex", apex), ("Fall", fall),
                         ("Climb", climb), ("Attack", attack), ("Hurt", hurt) })
            {
                var st = sm.AddState(n);
                st.motion = c;
                if (n == "Idle") sm.defaultState = st;
            }
            return ctrl;
        }

        // ------------------------------------------------------------------ prefabs
        public static void BuildPrefabs()
        {
            EnsureFolder(Prefabs);
            var noFriction = new PhysicsMaterial2D("NoFriction") { friction = 0f, bounciness = 0f };
            string matPath = Root + "/Physics/NoFriction.physicsMaterial2D";
            EnsureFolder(Root + "/Physics");
            AssetDatabase.DeleteAsset(matPath);
            AssetDatabase.CreateAsset(noFriction, matPath);

            var fire = ArtSetup.GetSprite("Environment/Forest/Forest_Tiles.png", "Forest_Fire");
            var fireBall = ArtSetup.GetSprite("Environment/Forest/Forest_Tiles.png", "Forest_FireBall");

            // Projectiles
            var slash = ProjectilePrefab("PlayerProjectile", Layers.PlayerProjectile,
                ArtSetup.GetSprite("FX/Hero_SlashFX.png", "Slash"), Color.white, Vector3.one, 16f, 0.9f, 0.3f,
                new Color(0.75f, 0.85f, 1f), true);
            var ghostBolt = ProjectilePrefab("GhostBolt", Layers.EnemyProjectile, fireBall, new Color(0.8f, 0.5f, 1f),
                Vector3.one * 1.5f, 6.5f, 4f, 0.2f, new Color(0.8f, 0.5f, 1f), false);
            var fireball = ProjectilePrefab("BossFireball", Layers.EnemyProjectile, fireBall, Color.white,
                Vector3.one * 2f, 8.5f, 5f, 0.3f, new Color(1f, 0.6f, 0.2f), false);
            var fallingFire = ProjectilePrefab("BossFallingFire", Layers.EnemyProjectile, fireBall, new Color(1f, 0.85f, 0.6f),
                new Vector3(2f, 2.5f, 1f), 11f, 4f, 0.3f, new Color(1f, 0.6f, 0.2f), false);
            var shockwave = ProjectilePrefab("BossShockwave", Layers.EnemyProjectile, fireBall, new Color(1f, 0.5f, 0.4f),
                new Vector3(2.5f, 1.5f, 1f), 7f, 2.5f, 0.25f, new Color(1f, 0.5f, 0.3f), false);

            BuildPlayer(slash, noFriction);

            // Enemies
            {
                var go = EnemyRoot("Skeleton", 3, new Vector2(0.8f, 1.4f), new Vector2(0f, 0.7f));
                var v = Child(go, "Visual");
                var sr = AddSprite(v, null, "Enemy");
                Animate(v, ArtSetup.Frames("Enemies/Enemy_skeleton.png", "Skeleton", 4), 6f);
                go.GetComponent<EnemyHealth>().spriteRenderer = sr;
                var patrol = go.AddComponent<PatrolEnemy>();
                patrol.visual = v.transform; patrol.speed = 1.6f; patrol.halfWidth = 0.4f; patrol.spriteFacesRight = false;
                Save<Transform>(go, "Enemy_Skeleton");
            }
            {
                var go = EnemyRoot("Slime", 2, new Vector2(0.85f, 0.85f), new Vector2(0f, 0.42f));
                var v = Child(go, "Visual");
                var sr = AddSprite(v, null, "Enemy");
                Animate(v, ArtSetup.Frames("Enemies/Enemy_slime.png", "Slime", 4), 5f);
                go.GetComponent<EnemyHealth>().spriteRenderer = sr;
                go.GetComponent<EnemyHealth>().deathColor = new Color(1f, 0.6f, 0.8f);
                Save<Transform>(go, "Enemy_Slime");
            }
            {
                var go = EnemyRoot("Ghost", 2, new Vector2(0.7f, 1.2f), Vector2.zero);
                var v = Child(go, "Visual");
                var sr = AddSprite(v, null, "Enemy", 0, unlit: true);
                sr.color = Color.white;
                Animate(v, ArtSetup.Frames("Enemies/Enemy_ghost.png", "Ghost", 2), 3f);
                go.GetComponent<EnemyHealth>().spriteRenderer = sr;
                var shooter = go.AddComponent<ShooterEnemy>();
                shooter.projectilePrefab = ghostBolt; shooter.spriteRenderer = sr; shooter.muzzleOffset = Vector2.zero;
                var bob = go.AddComponent<Bobbing>(); bob.amplitude = new Vector2(0f, 0.25f); bob.frequency = 0.5f;
                Save<Transform>(go, "Enemy_Ghost");
            }
            {
                var go = EnemyRoot("Bat", 1, new Vector2(1.0f, 0.7f), Vector2.zero);
                var v = Child(go, "Visual");
                var sr = AddSprite(v, null, "Enemy");
                Animate(v, ArtSetup.Frames("Enemies/Enemy_bat.png", "Bat", 3), 10f);
                go.GetComponent<EnemyHealth>().spriteRenderer = sr;
                var fly = go.AddComponent<FlyingEnemy>(); fly.spriteRenderer = sr; fly.distance = 5f; fly.speed = 2.6f;
                Save<Transform>(go, "Enemy_Bat");
            }
            {
                var anchor = new GameObject("Spider");
                var thread = Child(anchor, "Thread");
                var tsr = AddSprite(thread, ArtSetup.SingleSprite("Generated/Pixel.png"), "Enemy", -1);
                tsr.color = new Color(0.85f, 0.85f, 0.95f, 0.6f);
                var body = EnemyRoot("Body", 2, new Vector2(0.9f, 0.6f), Vector2.zero);
                body.transform.SetParent(anchor.transform, false);
                var v = Child(body, "Visual");
                var sr = AddSprite(v, null, "Enemy");
                var frames = ArtSetup.Frames("Enemies/Enemy_spider.png", "Spider", 6);
                Animate(v, new[] { frames[2], frames[3] }, 4f);
                body.GetComponent<EnemyHealth>().spriteRenderer = sr;
                body.GetComponent<EnemyHealth>().deathColor = new Color(0.9f, 0.8f, 0.3f);
                var bob = body.AddComponent<Bobbing>(); bob.amplitude = new Vector2(0f, 1.0f); bob.frequency = 0.3f;
                var h = anchor.AddComponent<HangingThread>(); h.spider = body.transform; h.thread = tsr;
                Save<Transform>(anchor, "Enemy_Spider");
            }
            {
                var go = new GameObject("Spikes") { layer = Layers.Hazards };
                AddSprite(go, ArtSetup.GetSprite("Environment/Forest/Forest_Tiles.png", "Forest_Spike_0"), "Hazards");
                var c = go.AddComponent<BoxCollider2D>(); c.isTrigger = true; c.size = new Vector2(0.8f, 0.45f); c.offset = new Vector2(0f, 0.22f);
                go.AddComponent<ContactDamage>();
                Save<Transform>(go, "Hazard_Spikes");
            }

            // Pickups
            var gemColors = new Dictionary<string, Color>
            {
                { "Green", new Color(0.4f, 1f, 0.5f) }, { "Blue", new Color(0.4f, 0.8f, 1f) },
                { "Red", new Color(1f, 0.4f, 0.4f) }, { "Orange", new Color(1f, 0.75f, 0.3f) },
            };
            foreach (var kv in gemColors)
            {
                var go = PickupRoot("Gem_" + kv.Key, ArtSetup.GetSprite("Items/Gems_CaveTileset.png", "Gem_" + kv.Key), 0.4f);
                var gem = go.AddComponent<Gem>(); gem.sparkleColor = kv.Value; gem.sound = Sfx.Gem;
                Save<Transform>(go, "Gem_" + kv.Key);
            }
            {
                var go = PickupRoot("Key_Gold", ArtSetup.SingleSprite("Items/Key_03.png"), 0.5f);
                var k = go.AddComponent<KeyPickup>(); k.key = KeyType.Gold; k.sound = Sfx.Key; k.sparkleColor = new Color(1f, 0.85f, 0.3f);
                k.message = "Found the Gold Key! It opens a locked gate.";
                AddPointLight(Child(go, "Glow"), new Color(1f, 0.85f, 0.4f), 2.5f, 0.8f);
                Save<Transform>(go, "Key_Gold");
            }
            {
                var go = PickupRoot("Key_Treasure", ArtSetup.SingleSprite("Items/Key_05.png"), 0.5f);
                var k = go.AddComponent<KeyPickup>(); k.key = KeyType.Treasure; k.sound = Sfx.Key; k.sparkleColor = new Color(0.5f, 0.7f, 1f);
                k.message = "Found the Treasure Key! The chamber door awaits.";
                AddPointLight(Child(go, "Glow"), new Color(0.5f, 0.7f, 1f), 2.5f, 0.9f);
                Save<Transform>(go, "Key_Treasure");
            }
            {
                var go = PickupRoot("Heart", ArtSetup.SingleSprite("Generated/Heart_Full.png"), 0.4f);
                var h = go.AddComponent<HeartPickup>(); h.sound = Sfx.Heal; h.sparkleColor = new Color(1f, 0.4f, 0.5f);
                Save<Transform>(go, "Pickup_Heart");
            }

            var greenGem = LoadPrefab("Gem_Green").GetComponent<Gem>();
            var orangeGem = LoadPrefab("Gem_Orange").GetComponent<Gem>();
            ChestPrefab("Chest_Iron", "Iron", greenGem, 3, 1, false);
            ChestPrefab("Chest_Silver", "Silver", orangeGem, 6, 1, false);
            ChestPrefab("Chest_Gold", "Gold", orangeGem, 15, 0, true);

            // Checkpoint: stone column that lights a flame when activated
            {
                var go = new GameObject("Checkpoint") { layer = Layers.Interactables };
                var c = go.AddComponent<BoxCollider2D>(); c.isTrigger = true; c.size = new Vector2(1f, 2f); c.offset = new Vector2(0f, 1f);
                var cp = go.AddComponent<Checkpoint>();
                var column = Child(go, "Column");
                AddSprite(column, ArtSetup.GetSprite("Environment/Forest/Forest_Tiles.png", "Forest_Column"), "Interactables");
                var flame = Child(go, "Flame", new Vector3(0f, 1.45f, 0f));
                AddSprite(flame, fire, "Interactables", 1, unlit: true);
                flame.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
                var light = AddPointLight(flame, new Color(1f, 0.65f, 0.3f), 4.5f, 1.1f);
                flame.AddComponent<LightFlicker>();
                cp.activeVisual = flame;
                cp.activeLight = light;
                Save<Transform>(go, "Checkpoint");
            }

            // Portal
            {
                var go = new GameObject("ExitPortal") { layer = Layers.Interactables };
                var open = Enumerable.Range(0, 17).Select(i => ArtSetup.GetSprite("Portals/Portal_Open.png", "PortalOpen_" + i.ToString("00"))).ToArray();
                var loop = ArtSetup.Frames("Portals/Portal_Loop.png", "PortalLoop", 5);
                var sr = AddSprite(go, open[0], "Interactables", 0, unlit: true);
                var anim = Animate(go, new[] { open[0] }, 1f);
                anim.randomStartFrame = false;
                var c = go.AddComponent<BoxCollider2D>(); c.isTrigger = true; c.size = new Vector2(1.0f, 1.6f); c.offset = new Vector2(0f, 0.9f);
                var exit = go.AddComponent<LevelExit>(); exit.animator = anim; exit.openFrames = open; exit.loopFrames = loop;
                AddPointLight(Child(go, "Glow", new Vector3(0f, 1f, 0f)), new Color(0.4f, 1f, 0.5f), 4f, 0.9f).gameObject.AddComponent<LightFlicker>();
                _ = sr;
                Save<Transform>(go, "ExitPortal");
            }

            GatePrefab("Gate_Gold", ArtSetup.GetSprite("Environment/Castle/Castle_Tiles.png", "Castle_Fence"), Vector3.zero,
                KeyType.Gold, "Locked. Find the Gold Key.");
            GatePrefab("Gate_Treasure", ArtSetup.GetSprite("Environment/Castle/Castle_Tiles.png", "Castle_DoorWood"), new Vector3(1f, 0f, 0f),
                KeyType.Treasure, "Sealed. Only the Treasure Key opens this door.");

            // Torch
            {
                var go = new GameObject("Torch");
                AddSprite(go, ArtSetup.GetSprite("Environment/Castle/Castle_Tiles.png", "Castle_Torch"), "Background", 60, unlit: true);
                var l = AddPointLight(go, new Color(1f, 0.6f, 0.3f), 7f, 1.5f);
                go.AddComponent<LightFlicker>();
                _ = l;
                Save<Transform>(go, "Torch");
            }

            BuildBoss(fireball, fallingFire, shockwave);
            AssetDatabase.SaveAssets();
        }

        static Projectile ProjectilePrefab(string name, int layer, Sprite sprite, Color tint, Vector3 scale, float speed,
            float lifetime, float radius, Color impact, bool rotate)
        {
            var go = new GameObject(name) { layer = layer };
            var sr = AddSprite(go, sprite, "FX", 0, unlit: true);
            sr.color = tint;
            go.transform.localScale = scale;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = radius / Mathf.Max(scale.x, 0.01f);
            var p = go.AddComponent<Projectile>();
            p.speed = speed; p.lifetime = lifetime; p.impactColor = impact; p.rotateToDirection = rotate;
            if (!rotate) { var glow = AddPointLight(Child(go, "Glow"), impact, 2f, 0.7f); _ = glow; }
            return Save<Projectile>(go, name);
        }

        static void BuildPlayer(Projectile projectile, PhysicsMaterial2D mat)
        {
            var go = new GameObject("Player") { layer = Layers.Player, tag = "Player" };
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 4f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.sharedMaterial = mat;
            var cap = go.AddComponent<CapsuleCollider2D>();
            cap.direction = CapsuleDirection2D.Vertical;
            cap.size = new Vector2(0.7f, 1.5f);
            cap.offset = new Vector2(0f, 0.76f);
            cap.sharedMaterial = mat;
            var pc = go.AddComponent<PlayerController>();
            var health = go.AddComponent<PlayerHealth>();
            var visual = Child(go, "Visual");
            var sr = AddSprite(visual, ArtSetup.GetSprite("Player/Hero_Idle.png", "Hero_Idle_0"), "Player");
            var animator = visual.AddComponent<Animator>();
            animator.runtimeAnimatorController = BuildPlayerController();
            pc.visual = visual.transform;
            pc.animator = animator;
            pc.projectilePrefab = projectile;
            health.spriteRenderer = sr;
            Save<Transform>(go, "Player");
        }

        static GameObject EnemyRoot(string name, int hp, Vector2 size, Vector2 offset)
        {
            var go = new GameObject(name) { layer = Layers.Enemy };
            Kinematic(go);
            var c = go.AddComponent<BoxCollider2D>();
            c.isTrigger = true; c.size = size; c.offset = offset;
            var h = go.AddComponent<EnemyHealth>();
            h.maxHealth = hp;
            go.AddComponent<ContactDamage>();
            return go;
        }

        static GameObject PickupRoot(string name, Sprite sprite, float radius)
        {
            var go = new GameObject(name) { layer = Layers.Interactables };
            var v = Child(go, "Visual");
            AddSprite(v, sprite, "Interactables", 0, unlit: true);
            var bob = v.AddComponent<Bobbing>(); bob.amplitude = new Vector2(0f, 0.12f); bob.frequency = 0.8f;
            var c = go.AddComponent<CircleCollider2D>(); c.isTrigger = true; c.radius = radius;
            return go;
        }

        static void ChestPrefab(string name, string metal, Gem gem, int gems, int heal, bool final)
        {
            var go = new GameObject(name) { layer = Layers.Interactables };
            var frames = ArtSetup.Frames($"Items/Chest_{metal}.png", "Chest_" + metal, 3);
            var sr = AddSprite(go, frames[0], "Interactables");
            var c = go.AddComponent<BoxCollider2D>(); c.isTrigger = true; c.size = new Vector2(1.8f, 1.4f); c.offset = new Vector2(0f, 0.7f);
            var chest = go.AddComponent<TreasureChest>();
            chest.openFrames = new[] { frames[1], frames[2] };
            chest.spriteRenderer = sr;
            chest.gemPrefab = gem; chest.gemCount = gems; chest.healAmount = heal; chest.isFinalTreasure = final;
            chest.glow = AddPointLight(Child(go, "Glow", new Vector3(0f, 0.8f, 0f)), new Color(1f, 0.8f, 0.4f), final ? 6f : 2.5f, final ? 1.2f : 0.5f);
            Save<Transform>(go, name);
        }

        static void GatePrefab(string name, Sprite sprite, Vector3 spriteOffset, KeyType key, string message)
        {
            var go = new GameObject(name) { layer = Layers.Interactables };
            var trigger = go.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true; trigger.size = new Vector2(2.8f, 2f); trigger.offset = new Vector2(1f, 1f);
            var gate = go.AddComponent<LockedGate>();
            var visual = Child(go, "Visual");
            var sv = Child(visual, "Sprite", spriteOffset);
            AddSprite(sv, sprite, "Platforms", 5);
            var solid = Child(go, "Solid");
            solid.layer = Layers.Ground;
            var sc = solid.AddComponent<BoxCollider2D>(); sc.size = new Vector2(2f, 2f); sc.offset = new Vector2(1f, 1f);
            gate.solidCollider = sc; gate.visual = visual.transform; gate.requiredKey = key; gate.lockedMessage = message;
            Save<Transform>(go, name);
        }

        static void BuildBoss(Projectile fireball, Projectile fallingFire, Projectile shockwave)
        {
            var go = new GameObject("DragonBoss") { layer = Layers.Enemy };
            Kinematic(go);
            var hurt = go.AddComponent<BoxCollider2D>();
            hurt.isTrigger = true; hurt.size = new Vector2(4.6f, 2.2f); hurt.offset = new Vector2(0f, 1.3f);
            var contact = go.AddComponent<ContactDamage>();
            var visual = Child(go, "Visual");
            var a = ArtSetup.SingleSprite("Boss/Dragon_A.png");
            var b = ArtSetup.SingleSprite("Boss/Dragon_B.png");
            var sr = AddSprite(visual, a, "Enemy", 5);
            Animate(visual, new[] { a, b }, 3f);
            AddPointLight(Child(go, "Aura", new Vector3(0f, 1.5f, 0f)), new Color(1f, 0.5f, 0.3f), 5f, 0.6f);
            var boss = go.AddComponent<DragonBoss>();
            boss.visual = visual.transform; boss.spriteRenderer = sr; boss.contactDamage = contact; boss.hurtbox = hurt;
            boss.fireballPrefab = fireball; boss.fallingFirePrefab = fallingFire; boss.shockwavePrefab = shockwave;
            boss.warningMaterial = Unlit;
            boss.bossMusic = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/Music/Music_SlayTheEvil.ogg");
            Save<Transform>(go, "DragonBoss");
        }

        // ------------------------------------------------------------------ audio
        public static void BuildSfxLibrary()
        {
            EnsureFolder(Root + "/Resources");
            string path = Root + "/Resources/SfxLibrary.asset";
            var lib = AssetDatabase.LoadAssetAtPath<SfxLibrary>(path);
            if (lib == null) { lib = ScriptableObject.CreateInstance<SfxLibrary>(); AssetDatabase.CreateAsset(lib, path); }
            var map = new (Sfx id, string file, float vol)[]
            {
                (Sfx.Jump, "sfx_jump", 0.45f), (Sfx.Land, "sfx_land", 0.6f), (Sfx.Shoot, "sfx_shoot", 0.5f),
                (Sfx.Gem, "sfx_gem", 0.6f), (Sfx.Key, "sfx_key", 0.8f), (Sfx.Chest, "sfx_chest", 0.8f),
                (Sfx.Unlock, "sfx_unlock", 0.7f), (Sfx.Locked, "sfx_locked", 0.6f), (Sfx.EnemyHit, "sfx_enemy_hit", 0.7f),
                (Sfx.EnemyDie, "sfx_enemy_die", 0.8f), (Sfx.PlayerHurt, "sfx_player_hurt", 0.9f), (Sfx.PlayerDie, "sfx_player_die", 0.8f),
                (Sfx.Checkpoint, "sfx_checkpoint", 0.7f), (Sfx.Portal, "sfx_portal", 0.7f), (Sfx.UIClick, "sfx_ui_click", 0.6f),
                (Sfx.UISelect, "sfx_ui_select", 0.4f), (Sfx.BossWarning, "sfx_boss_warning", 0.7f), (Sfx.BossHit, "sfx_boss_hit", 0.6f),
                (Sfx.EnemyShoot, "sfx_enemy_shoot", 0.6f), (Sfx.RockImpact, "sfx_rock_impact", 0.9f), (Sfx.BossDie, "sfx_boss_die", 1f),
                (Sfx.Victory, "sfx_victory", 0.9f), (Sfx.ProjectileHit, "sfx_projectile_hit", 0.5f), (Sfx.Heal, "sfx_heal", 0.7f),
            };
            lib.entries = map.Select(m => new SfxLibrary.Entry
            {
                id = m.id, volume = m.vol, pitchVariance = 0.06f,
                clips = new[] { AssetDatabase.LoadAssetAtPath<AudioClip>($"{Root}/Audio/SFX/{m.file}.ogg") },
            }).ToArray();
            EditorUtility.SetDirty(lib);

            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { Root + "/Audio/Music" }))
            {
                var imp = (AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                var s = imp.defaultSampleSettings;
                s.loadType = AudioClipLoadType.Streaming;
                imp.defaultSampleSettings = s;
                imp.SaveAndReimport();
            }
            AssetDatabase.SaveAssets();
        }
    }
}
