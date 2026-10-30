using System.Collections;
using UnityEngine;

namespace TileVania
{
    /// <summary>
    /// Hovering ranged enemy: when the player is in range and visible it glows (telegraph),
    /// then fires a projectile aimed at the player.
    /// </summary>
    public class ShooterEnemy : MonoBehaviour
    {
        public Projectile projectilePrefab;
        public float range = 9f;
        public float cooldown = 2.4f;
        public float telegraphTime = 0.6f;
        public Vector2 muzzleOffset = new Vector2(0f, 0.7f);
        public SpriteRenderer spriteRenderer;
        public bool spriteFacesRight = false;
        public Color telegraphColor = new Color(1f, 0.55f, 1f);

        float timer;
        Transform player;

        void Start()
        {
            timer = cooldown * Random.Range(0.4f, 0.8f);
            var pc = FindFirstObjectByType<PlayerController>();
            if (pc != null) player = pc.transform;
        }

        void Update()
        {
            if (player == null) return;
            Vector2 muzzle = (Vector2)transform.position + muzzleOffset;
            Vector2 target = (Vector2)player.position + Vector2.up * 0.8f;
            if (spriteRenderer != null)
                spriteRenderer.flipX = spriteFacesRight ? target.x < muzzle.x : target.x > muzzle.x;

            timer -= Time.deltaTime;
            if (timer > 0f) return;
            if (Vector2.Distance(muzzle, target) > range) return;
            if (Physics2D.Linecast(muzzle, target, Layers.SolidMask)) return;
            timer = cooldown;
            StartCoroutine(Fire());
        }

        IEnumerator Fire()
        {
            AudioManager.Play(Sfx.EnemyShoot, 0.5f);
            for (float t = 0; t < telegraphTime; t += Time.deltaTime)
            {
                if (spriteRenderer != null)
                    spriteRenderer.color = Color.Lerp(Color.white, telegraphColor, Mathf.PingPong(t * 8f, 1f));
                yield return null;
            }
            if (spriteRenderer != null) spriteRenderer.color = Color.white;
            if (player == null || projectilePrefab == null) yield break;
            Vector2 muzzle = (Vector2)transform.position + muzzleOffset;
            Vector2 target = (Vector2)player.position + Vector2.up * 0.8f;
            var p = Instantiate(projectilePrefab, muzzle, Quaternion.identity);
            p.Launch(target - muzzle);
        }
    }
}
