using UnityEngine;

namespace TileVania
{
    /// <summary>Hurts the player while touching this trigger (spikes, enemy bodies, the boss).</summary>
    public class ContactDamage : MonoBehaviour
    {
        public int damage = 1;
        public bool active = true;

        void OnTriggerStay2D(Collider2D other) => TryHit(other);
        void OnCollisionStay2D(Collision2D c) => TryHit(c.collider);

        void TryHit(Collider2D other)
        {
            if (!active || !enabled) return;
            var player = other.attachedRigidbody != null ? other.attachedRigidbody.GetComponent<PlayerHealth>() : null;
            if (player != null) player.TakeDamage(new DamageInfo(damage, transform.position));
        }
    }
}
