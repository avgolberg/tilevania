using System.IO;
using UnityEditor;
using UnityEngine;

namespace TileVania.EditorTools
{
    /// <summary>
    /// Tiny original pixel-art pieces that the approved packs do not provide
    /// (heart icons, a ladder tile, a 1-pixel sprite). Drawn from code so they are clearly project-made.
    /// </summary>
    public static class GeneratedArt
    {
        public const string Folder = ArtSetup.Art + "/Generated";

        static readonly string[] Heart =
        {
            ".XX...XX.",
            "XRRX.XRRX",
            "XRWRXRRRX",
            "XRRRRRRRX",
            "XRRRRRRRX",
            ".XRRRRRX.",
            "..XRRRX..",
            "...XRX...",
            "....X....",
        };

        public static void CreateAll()
        {
            Directory.CreateDirectory(Folder);
            Write("Heart_Full.png", FromPattern(Heart, c => c == 'X' ? Hex("2b0f17") : c == 'R' ? Hex("e43b44") : c == 'W' ? Hex("ffd6d6") : Color.clear));
            Write("Heart_Empty.png", FromPattern(Heart, c => c == 'X' ? Hex("2b0f17") : c == '.' ? Color.clear : Hex("4a3a4d")));
            Write("Ladder.png", LadderTexture());
            var px = new Texture2D(4, 4);
            px.SetPixels(new Color[16].Populate(Color.white));
            Write("Pixel.png", px);
            AssetDatabase.Refresh();

            ArtSetup.Single("Generated/Heart_Full.png", 12, new Vector2(0.5f, 0.5f));
            ArtSetup.Single("Generated/Heart_Empty.png", 12, new Vector2(0.5f, 0.5f));
            ArtSetup.Single("Generated/Ladder.png", 16, new Vector2(0.5f, 0.5f));
            ArtSetup.Single("Generated/Pixel.png", 64, new Vector2(0.5f, 0.5f), fullRect: true);
        }

        static Texture2D LadderTexture()
        {
            var t = new Texture2D(16, 16);
            var dark = Hex("2a160c"); var wood = Hex("7a4a28"); var light = Hex("a8703c");
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    Color c = Color.clear;
                    bool rail = x == 2 || x == 3 || x == 12 || x == 13;
                    bool railEdge = x == 1 || x == 4 || x == 11 || x == 14;
                    int ry = y % 8;
                    bool rung = (ry == 3 || ry == 4) && x > 3 && x < 12;
                    bool rungEdge = (ry == 2 || ry == 5) && x > 3 && x < 12;
                    if (rail) c = x == 2 || x == 12 ? light : wood;
                    else if (rung) c = ry == 4 ? wood : light;
                    else if (railEdge || rungEdge) c = dark;
                    t.SetPixel(x, y, c);
                }
            return t;
        }

        static Texture2D FromPattern(string[] rows, System.Func<char, Color> map)
        {
            int h = rows.Length, w = rows[0].Length;
            var t = new Texture2D(w, h);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    t.SetPixel(x, h - 1 - y, map(rows[y][x]));
            return t;
        }

        static void Write(string file, Texture2D t)
        {
            t.Apply();
            File.WriteAllBytes(Path.Combine(Folder, file), t.EncodeToPNG());
            Object.DestroyImmediate(t);
        }

        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }

        static Color[] Populate(this Color[] arr, Color c)
        {
            for (int i = 0; i < arr.Length; i++) arr[i] = c;
            return arr;
        }
    }
}
