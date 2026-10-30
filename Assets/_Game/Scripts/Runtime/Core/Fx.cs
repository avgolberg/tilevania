using System.Collections.Generic;
using UnityEngine;

namespace TileVania
{
    /// <summary>
    /// Lightweight pooled pixel-particle bursts (dust, sparkles, hit sparks, explosions).
    /// Uses plain SpriteRenderers so it works with the URP 2D renderer without particle materials.
    /// </summary>
    public class Fx : MonoBehaviour
    {
        class Particle
        {
            public SpriteRenderer sr;
            public Vector2 velocity;
            public float life, maxLife, gravity, startSize;
            public Color color;
        }

        static Fx instance;
        static Sprite pixel;
        static Material litMaterial;

        readonly List<Particle> active = new List<Particle>();
        readonly Stack<Particle> pool = new Stack<Particle>();

        static Fx Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("[Fx]");
                    instance = go.AddComponent<Fx>();
                }
                return instance;
            }
        }

        /// <summary>Material for runtime sprites; registered by the level so particles match scene lighting.</summary>
        public static void RegisterMaterial(Material m) { if (m != null) litMaterial = m; }

        public static Sprite PixelSprite
        {
            get
            {
                if (pixel == null)
                {
                    var tex = new Texture2D(1, 1) { filterMode = FilterMode.Point };
                    tex.SetPixel(0, 0, Color.white);
                    tex.Apply();
                    pixel = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 16f);
                }
                return pixel;
            }
        }

        public static void Burst(Vector2 pos, Color color, int count, float speed, float life = 0.5f,
            float gravity = 8f, float size = 2f)
        {
            var fx = Instance;
            for (int i = 0; i < count; i++)
            {
                var p = fx.Get();
                Vector2 dir = Random.insideUnitCircle.normalized;
                p.velocity = dir * speed * Random.Range(0.4f, 1f) + Vector2.up * speed * 0.3f;
                p.maxLife = p.life = life * Random.Range(0.7f, 1.2f);
                p.gravity = gravity;
                p.color = color;
                p.startSize = size * Random.Range(0.7f, 1.2f);
                p.sr.transform.position = pos + Random.insideUnitCircle * 0.15f;
                p.sr.color = color;
                p.sr.transform.localScale = Vector3.one * p.startSize;
                p.sr.gameObject.SetActive(true);
                fx.active.Add(p);
            }
        }

        public static void Dust(Vector2 pos, int count = 6) =>
            Burst(pos, new Color(0.85f, 0.8f, 0.7f, 0.9f), count, 2.5f, 0.35f, 2f, 1.5f);

        Particle Get()
        {
            if (pool.Count > 0) return pool.Pop();
            var go = new GameObject("p");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PixelSprite;
            if (litMaterial != null) sr.sharedMaterial = litMaterial;
            sr.sortingLayerName = "FX";
            return new Particle { sr = sr };
        }

        void Update()
        {
            float dt = Time.deltaTime;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var p = active[i];
                p.life -= dt;
                if (p.life <= 0f)
                {
                    p.sr.gameObject.SetActive(false);
                    active.RemoveAt(i);
                    pool.Push(p);
                    continue;
                }
                p.velocity.y -= p.gravity * dt;
                p.sr.transform.position += (Vector3)(p.velocity * dt);
                float k = p.life / p.maxLife;
                var c = p.color; c.a *= k;
                p.sr.color = c;
                p.sr.transform.localScale = Vector3.one * Mathf.Max(1f, p.startSize * k);
            }
        }

        void OnDestroy() { if (instance == this) instance = null; }
    }
}
