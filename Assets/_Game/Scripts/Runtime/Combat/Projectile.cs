using UnityEngine;

namespace TileVania
{
    /// <summary>
    /// Straight-flying projectile on its own physics layer (PlayerProjectile or EnemyProjectile).
    /// The layer matrix limits what it can touch; it is destroyed by the first wall or target.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Projectile : MonoBehaviour
    {
        public int damage = 1;
        public float speed = 16f;
        public float lifetime = 1.6f;
        public Color impactColor = new Color(0.8f, 0.9f, 1f);
        public bool rotateToDirection = true;
        public Sfx impactSound = Sfx.ProjectileHit;

        Rigidbody2D rb;
        bool spent;

        public void Launch(Vector2 direction)
        {
            rb = GetComponent<Rigidbody2D>();
            // Dynamic (gravity 0) so trigger contacts are reported against static walls and kinematic enemies.
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.linearVelocity = direction.normalized * speed;
            if (rotateToDirection)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }
            Destroy(gameObject, lifetime);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (spent) return;
            int layer = other.gameObject.layer;
            if (layer == Layers.Ground)
            {
                Impact();
                return;
            }
            var target = other.GetComponentInParent<IDamageable>();
            if (target == null) return;
            if (target.TakeDamage(new DamageInfo(damage, transform.position)))
                Impact();
        }

        void Impact()
        {
            spent = true;
            Fx.Burst(transform.position, impactColor, 6, 3f, 0.25f, 0f, 1.5f);
            AudioManager.Play(impactSound, 0.6f);
            Destroy(gameObject);
        }
    }
}
