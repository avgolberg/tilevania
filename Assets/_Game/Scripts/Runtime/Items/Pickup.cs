using UnityEngine;

namespace TileVania
{
    /// <summary>
    /// Base for collectibles. Detects the player by component (not by tag) and collects only once,
    /// even though several colliders may touch it in the same frame.
    /// </summary>
    public abstract class Pickup : MonoBehaviour
    {
        public Sfx sound = Sfx.Gem;
        public Color sparkleColor = Color.white;

        [Tooltip("Spawned by a chest: pop out, then fly to the player automatically.")]
        public bool magnetToPlayer;

        bool collected;
        float magnetDelay = 0.45f;
        Vector2 popVelocity;
        PlayerController magnetTarget;

        protected virtual void Start()
        {
            if (magnetToPlayer)
            {
                popVelocity = new Vector2(Random.Range(-3f, 3f), Random.Range(5f, 8f));
                magnetTarget = FindFirstObjectByType<PlayerController>();
                var bob = GetComponent<Bobbing>();
                if (bob) bob.enabled = false;
            }
        }

        void Update()
        {
            if (!magnetToPlayer || collected) return;
            if (magnetDelay > 0f)
            {
                magnetDelay -= Time.deltaTime;
                popVelocity.y -= 18f * Time.deltaTime;
                transform.position += (Vector3)(popVelocity * Time.deltaTime);
                return;
            }
            if (magnetTarget == null) { Collect(null); return; }
            Vector3 goal = magnetTarget.transform.position + Vector3.up * 0.8f;
            transform.position = Vector3.MoveTowards(transform.position, goal, 14f * Time.deltaTime);
            if ((transform.position - goal).sqrMagnitude < 0.04f) Collect(magnetTarget);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (collected || (magnetToPlayer && magnetDelay > 0f)) return;
            var player = other.attachedRigidbody != null ? other.attachedRigidbody.GetComponent<PlayerController>() : null;
            if (player == null) return;
            var health = player.GetComponent<PlayerHealth>();
            if (health != null && health.IsDead) return;
            if (!CanCollect(player)) return;
            Collect(player);
        }

        void Collect(PlayerController player)
        {
            if (collected) return;
            collected = true;
            OnCollected(player);
            AudioManager.Play(sound);
            Fx.Burst(transform.position, sparkleColor, 10, 3.5f, 0.45f, 2f, 1.5f);
            Destroy(gameObject);
        }

        protected virtual bool CanCollect(PlayerController player) => true;
        protected abstract void OnCollected(PlayerController player);
    }
}
