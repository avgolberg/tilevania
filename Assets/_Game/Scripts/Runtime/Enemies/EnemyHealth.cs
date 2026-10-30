using System;
using System.Collections;
using UnityEngine;

namespace TileVania
{
    /// <summary>
    /// Enemy hit points. Damage arrives through a single hurtbox collider, so hits register
    /// the same from any direction.
    /// </summary>
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        public int maxHealth = 2;
        public SpriteRenderer spriteRenderer;
        public Color deathColor = new Color(0.9f, 0.9f, 0.9f);
        public Pickup dropOnDeath;

        int health;
        bool dead;
        Coroutine flash;

        public bool IsDead => dead;
        public event Action Died;

        void Awake() => health = maxHealth;

        public bool TakeDamage(DamageInfo info)
        {
            if (dead) return false;
            health -= info.amount;
            if (health <= 0) { Die(); return true; }
            AudioManager.Play(Sfx.EnemyHit);
            if (flash != null) StopCoroutine(flash);
            flash = StartCoroutine(Flash());
            return true;
        }

        IEnumerator Flash()
        {
            if (spriteRenderer == null) yield break;
            spriteRenderer.color = new Color(1f, 0.35f, 0.35f);
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = Color.white;
            flash = null;
        }

        void Die()
        {
            dead = true;
            AudioManager.Play(Sfx.EnemyDie);
            Vector2 center = spriteRenderer != null ? (Vector2)spriteRenderer.bounds.center : (Vector2)transform.position;
            Fx.Burst(center, deathColor, 18, 5f, 0.6f);
            CameraController.Shake(0.1f, 0.15f);
            if (dropOnDeath != null) Instantiate(dropOnDeath, center, Quaternion.identity);
            Died?.Invoke();
            Destroy(gameObject);
        }
    }
}
