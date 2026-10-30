using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace TileVania.EditorTools
{
    /// <summary>Builds the in-game canvas prefab (HUD + menus) and the main menu canvas.</summary>
    public static class UIBuilder
    {
        static readonly Color PanelColor = new Color(0.09f, 0.06f, 0.14f, 0.94f);
        static readonly Color ButtonColor = new Color(0.2f, 0.15f, 0.3f, 1f);
        static readonly Color Gold = new Color(1f, 0.84f, 0.36f);
        static readonly Color Text = new Color(0.95f, 0.92f, 0.85f);

        static TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

        static Material titleMaterial;

        static Material TitleMaterial
        {
            get
            {
                if (titleMaterial != null) return titleMaterial;
                string path = ContentBuilder.Root + "/UI/Title_Outline.mat";
                ContentBuilder.EnsureFolder(ContentBuilder.Root + "/UI");
                titleMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (titleMaterial == null)
                {
                    titleMaterial = new Material(Font.material);
                    AssetDatabase.CreateAsset(titleMaterial, path);
                }
                titleMaterial.SetFloat("_OutlineWidth", 0.18f);
                titleMaterial.SetColor("_OutlineColor", new Color(0.12f, 0.05f, 0.1f));
                titleMaterial.EnableKeyword("OUTLINE_ON");
                titleMaterial.EnableKeyword("UNDERLAY_ON");
                titleMaterial.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, 0.8f));
                titleMaterial.SetFloat("_UnderlayOffsetX", 0.6f);
                titleMaterial.SetFloat("_UnderlayOffsetY", -0.6f);
                EditorUtility.SetDirty(titleMaterial);
                return titleMaterial;
            }
        }

        static Sprite UISprite => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        static Sprite Pixel => ArtSetup.SingleSprite("Generated/Pixel.png");

        // ------------------------------------------------------------------ primitives
        public static RectTransform Rect(GameObject parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            go.layer = 5;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        static RectTransform Stretch(GameObject parent, string name)
        {
            var rt = Rect(parent, name, Vector2.zero, Vector2.zero, Vector2.zero);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        static Image Img(RectTransform rt, Sprite s, Color c, bool sliced = false)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = s; img.color = c;
            img.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            img.preserveAspect = !sliced && s != null && s != Pixel;
            img.raycastTarget = false;
            return img;
        }

        public static TMP_Text Label(GameObject parent, string name, string text, float size, Color color,
            Vector2 anchor, Vector2 pos, Vector2 box, TextAlignmentOptions align = TextAlignmentOptions.Center, bool title = false)
        {
            var rt = Rect(parent, name, anchor, pos, box);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = Font;
            if (title) t.fontSharedMaterial = TitleMaterial;
            t.text = text; t.fontSize = size; t.color = color; t.alignment = align;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal;
            return t;
        }

        static Button MakeButton(GameObject parent, string label)
        {
            var rt = Rect(parent, "Button_" + label.Replace(" ", ""), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(380, 66));
            var img = Img(rt, UISprite, Color.white, true);
            img.raycastTarget = true;
            var b = rt.gameObject.AddComponent<Button>();
            var colors = b.colors;
            colors.normalColor = ButtonColor;
            colors.highlightedColor = new Color(0.55f, 0.4f, 0.16f);
            colors.selectedColor = new Color(0.55f, 0.4f, 0.16f);
            colors.pressedColor = new Color(0.75f, 0.58f, 0.2f);
            colors.fadeDuration = 0.08f;
            b.colors = colors;
            b.targetGraphic = img;
            var le = rt.gameObject.AddComponent<LayoutElement>(); le.preferredHeight = 66; le.preferredWidth = 380;
            var t = Label(rt.gameObject, "Text", label, 34, Text, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(380, 66));
            t.fontStyle = FontStyles.Bold;
            rt.gameObject.AddComponent<UIButtonSound>();
            return b;
        }

        static Slider MakeVolumeRow(GameObject parent, string label, VolumeSlider.Channel channel)
        {
            var row = Rect(parent, "Row_" + label.Replace(" ", ""), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(460, 56));
            var le = row.gameObject.AddComponent<LayoutElement>(); le.preferredHeight = 56; le.preferredWidth = 460;
            Label(row.gameObject, "Label", label, 28, Text, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(170, 50), TextAlignmentOptions.Left);
            var res = new DefaultControls.Resources
            {
                standard = UISprite,
                background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
                knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            };
            var sgo = DefaultControls.CreateSlider(res);
            sgo.transform.SetParent(row, false);
            var srt = (RectTransform)sgo.transform;
            srt.anchorMin = srt.anchorMax = srt.pivot = new Vector2(1f, 0.5f);
            srt.anchoredPosition = Vector2.zero;
            srt.sizeDelta = new Vector2(270, 26);
            SetLayerRecursive(sgo, 5);
            var slider = sgo.GetComponent<Slider>();
            var colors = slider.colors;
            colors.selectedColor = Gold; colors.highlightedColor = Gold;
            slider.colors = colors;
            sgo.transform.Find("Fill Area/Fill").GetComponent<Image>().color = new Color(0.85f, 0.6f, 0.2f);
            var vs = sgo.AddComponent<VolumeSlider>();
            vs.channel = channel;
            return slider;
        }

        static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform t in go.transform) SetLayerRecursive(t.gameObject, layer);
        }

        static GameObject Panel(GameObject canvas, string name, string title, Color titleColor, string subtitle, float height)
        {
            var dim = Stretch(canvas, name);
            Img(dim, Pixel, new Color(0f, 0f, 0f, 0.55f)).raycastTarget = true;
            var box = Rect(dim.gameObject, "Box", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620, height));
            Img(box, UISprite, PanelColor, true);
            var border = Rect(box.gameObject, "Border", new Vector2(0.5f, 1f), new Vector2(0, -6), new Vector2(600, 4));
            Img(border, Pixel, Gold * new Color(1, 1, 1, 0.8f));
            Label(box.gameObject, "Title", title, 64, titleColor, new Vector2(0.5f, 1f), new Vector2(0, -30), new Vector2(600, 90), title: true);
            if (!string.IsNullOrEmpty(subtitle))
                Label(box.gameObject, "Subtitle", subtitle, 28, Text * new Color(1, 1, 1, 0.8f), new Vector2(0.5f, 1f), new Vector2(0, -115), new Vector2(560, 50));
            var content = Rect(box.gameObject, "Content", new Vector2(0.5f, 1f), new Vector2(0, string.IsNullOrEmpty(subtitle) ? -140 : -180),
                new Vector2(560, height - 200));
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 16; layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = false; layout.childControlWidth = false;
            layout.childForceExpandHeight = false; layout.childForceExpandWidth = false;
            return content.gameObject;
        }

        static GameObject Canvas(string name, int order)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = 5 };
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = order;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return go;
        }

        public static GameObject EventSystemObject()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>(); // default UI actions are assigned at runtime
            return go;
        }

        // ------------------------------------------------------------------ in-game canvas prefab
        public static GameObject BuildGameCanvasPrefab()
        {
            var canvas = Canvas("GameCanvas", 10);
            var hud = canvas.AddComponent<HUD>();
            var hudRoot = Stretch(canvas, "HUD").gameObject;

            hud.heartFull = ArtSetup.SingleSprite("Generated/Heart_Full.png");
            hud.heartEmpty = ArtSetup.SingleSprite("Generated/Heart_Empty.png");
            hud.hearts = new Image[3];
            for (int i = 0; i < 3; i++)
                hud.hearts[i] = Img(Rect(hudRoot, "Heart" + i, new Vector2(0, 1), new Vector2(36 + i * 66, -30), new Vector2(60, 60)), hud.heartFull, Color.white);

            Img(Rect(hudRoot, "GemIcon", new Vector2(0, 1), new Vector2(40, -106), new Vector2(48, 48)),
                ArtSetup.GetSprite("Items/Gems_CaveTileset.png", "Gem_Green"), Color.white);
            hud.gemText = Label(hudRoot, "GemCount", "0", 44, Gold, new Vector2(0, 1), new Vector2(100, -100), new Vector2(200, 60), TextAlignmentOptions.Left, true);
            hud.goldKeyIcon = Img(Rect(hudRoot, "GoldKey", new Vector2(0, 1), new Vector2(36, -166), new Vector2(56, 56)),
                ArtSetup.SingleSprite("Items/Key_03.png"), Color.white).gameObject;
            hud.treasureKeyIcon = Img(Rect(hudRoot, "TreasureKey", new Vector2(0, 1), new Vector2(100, -166), new Vector2(56, 56)),
                ArtSetup.SingleSprite("Items/Key_05.png"), Color.white).gameObject;

            var title = Rect(hudRoot, "TitleCard", new Vector2(0.5f, 1f), new Vector2(0, -170), new Vector2(1200, 200));
            hud.titleGroup = title.gameObject.AddComponent<CanvasGroup>();
            hud.titleText = Label(title.gameObject, "Title", "Level", 84, Gold, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1200, 110), title: true);
            hud.subtitleText = Label(title.gameObject, "Subtitle", "", 36, Text, new Vector2(0.5f, 1f), new Vector2(0, -110), new Vector2(1200, 60), title: true);

            var msg = Rect(hudRoot, "Message", new Vector2(0.5f, 0f), new Vector2(0, 110), new Vector2(1000, 70));
            Img(msg, UISprite, new Color(0.05f, 0.03f, 0.08f, 0.75f), true);
            hud.messageGroup = msg.gameObject.AddComponent<CanvasGroup>();
            hud.messageText = Label(msg.gameObject, "Text", "", 34, Text, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(980, 64));

            var boss = Rect(hudRoot, "BossBar", new Vector2(0.5f, 1f), new Vector2(0, -40), new Vector2(900, 90));
            hud.bossBarRoot = boss.gameObject;
            hud.bossNameText = Label(boss.gameObject, "Name", "Guardian of the Treasure", 34, Gold, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(900, 44), title: true);
            Img(Rect(boss.gameObject, "Back", new Vector2(0.5f, 0f), new Vector2(0, 6), new Vector2(880, 34)), UISprite, new Color(0.05f, 0.02f, 0.05f, 0.9f), true);
            var fill = Img(Rect(boss.gameObject, "Fill", new Vector2(0.5f, 0f), new Vector2(0, 12), new Vector2(868, 22)), Pixel, new Color(0.85f, 0.18f, 0.18f));
            fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillAmount = 1f;
            hud.bossBarFill = fill;

            var menus = canvas.AddComponent<GameMenus>();
            var pause = Panel(canvas, "PausePanel", "Paused", Gold, null, 640);
            menus.pausePanel = pause.transform.parent.parent.gameObject;
            menus.resumeButton = MakeButton(pause, "Resume");
            menus.restartButton = MakeButton(pause, "Restart Level");
            MakeVolumeRow(pause, "Music", VolumeSlider.Channel.Music);
            MakeVolumeRow(pause, "Sound FX", VolumeSlider.Channel.Sfx);
            menus.pauseMenuButton = MakeButton(pause, "Main Menu");

            var death = Panel(canvas, "DeathPanel", "You Fell", new Color(1f, 0.4f, 0.35f), "The treasure still awaits...", 480);
            menus.deathPanel = death.transform.parent.parent.gameObject;
            menus.retryButton = MakeButton(death, "Retry from Checkpoint");
            menus.deathMenuButton = MakeButton(death, "Main Menu");

            var victory = Panel(canvas, "VictoryPanel", "Victory!", Gold, "You claimed the Guardian's treasure.", 560);
            menus.victoryPanel = victory.transform.parent.parent.gameObject;
            menus.victoryStats = Label(victory, "Stats", "", 32, Text, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520, 150));
            menus.victoryStats.gameObject.AddComponent<LayoutElement>().preferredHeight = 150;
            menus.victoryMenuButton = MakeButton(victory, "Main Menu");

            string path = ContentBuilder.Prefabs + "/GameCanvas.prefab";
            PrefabUtility.SaveAsPrefabAsset(canvas, path);
            Object.DestroyImmediate(canvas);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        // ------------------------------------------------------------------ main menu
        public static void BuildMainMenuCanvas(MainMenuController controller)
        {
            var canvas = Canvas("MenuCanvas", 10);
            Label(canvas, "Title", "TileVania", 150, Gold, new Vector2(0.5f, 1f), new Vector2(0, -90), new Vector2(1400, 180), title: true);
            Label(canvas, "Subtitle", "Treasure Hunt", 56, Text, new Vector2(0.5f, 1f), new Vector2(0, -260), new Vector2(1200, 80), title: true);

            var main = Rect(canvas, "MainPanel", new Vector2(0.5f, 0.5f), new Vector2(0, -120), new Vector2(420, 360));
            var layout = main.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 18; layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = false; layout.childControlWidth = false;
            layout.childForceExpandHeight = false; layout.childForceExpandWidth = false;
            controller.mainPanel = main.gameObject;
            controller.playButton = MakeButton(main.gameObject, "Play");
            controller.settingsButton = MakeButton(main.gameObject, "Settings");
            controller.creditsButton = MakeButton(main.gameObject, "Credits");
            controller.quitButton = MakeButton(main.gameObject, "Quit");

            var settings = Panel(canvas, "SettingsPanel", "Settings", Gold, "Audio volume", 480);
            controller.settingsPanel = settings.transform.parent.parent.gameObject;
            MakeVolumeRow(settings, "Music", VolumeSlider.Channel.Music);
            MakeVolumeRow(settings, "Sound FX", VolumeSlider.Channel.Sfx);
            controller.settingsBackButton = MakeButton(settings, "Back");

            var credits = Panel(canvas, "CreditsPanel", "Credits", Gold, null, 820);
            controller.creditsPanel = credits.transform.parent.parent.gameObject;
            ((RectTransform)credits.transform.parent).sizeDelta = new Vector2(1100, 820);
            ((RectTransform)credits.transform).sizeDelta = new Vector2(1040, 620);
            var text = Label(credits, "Text",
                "<b>Hero</b> - Adventurer Hero by STAEL\n" +
                "<b>Forest, ruins, dragon</b> - Pixel Art Platformer Asset Pack by Glax (CC0)\n" +
                "<b>Castle</b> - Spooky Castle Tileset (OpenGameArt)\n" +
                "<b>Enemies</b> - 2D Platformer Enemies (OpenGameArt)\n" +
                "<b>Gems</b> - Cave Platformer Tileset by Rotting Pixels\n" +
                "<b>Keys</b> - CC0 Key Icons by 7Soul1\n" +
                "<b>Chests</b> - Animated Pixel Art Treasure Chests by Yevhen Babiichuk (DustDFG), CC-BY 4.0\n" +
                "<b>Portals</b> - Portals (OpenGameArt)\n" +
                "<b>Music</b> - \"8-bit Perilous Dungeon\" & \"8-bit Slay the Evil\" by HydroGene, \"Spooky Dungeon\" by Memoraphile\n" +
                "<b>Sound effects</b> - Kenney Impact & Interface Sounds (CC0)\n\n" +
                "Full details: Docs/ASSET_CREDITS.md",
                26, Text, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 520), TextAlignmentOptions.TopLeft);
            text.gameObject.AddComponent<LayoutElement>().preferredHeight = 520;
            controller.creditsBackButton = MakeButton(credits, "Back");

            Label(canvas, "Hint", "Move: A/D or Arrows   Jump: Space   Shoot: J / X / Left Click   Climb: W/S   Pause: Esc",
                24, Text * new Color(1, 1, 1, 0.7f), new Vector2(0.5f, 0f), new Vector2(0, 30), new Vector2(1800, 40));
        }
    }
}
