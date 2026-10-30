using System;
using System.Collections;
using UnityEngine;

namespace TileVania
{
    /// <summary>Three hearts, knockback, invulnerability blinking, and the death sequence.</summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        public int maxHearts = 3;
        public float invulnerabilityTime = 1.2f;
        public Vector2 knockback = new Vector2(7f, 9f);
        public SpriteRenderer spriteRenderer;

        PlayerController controller;
        Collider2D bodyCollider;
        float invulnerable;

        public int Hearts { get; private set; }
        public bool IsDead { get; private set; }
        public bool IsInvulnerable => invulnerable > 0f;

        public event Action<int, int> HeartsChanged;

        void Awake()
        {
            controller = GetComponent<PlayerController>();
            bodyCollider = GetComponent<Collider2D>();
            Hearts = maxHearts;
        }

        void Start() => HeartsChanged?.Invoke(Hearts, maxHearts);

        public bool TakeDamage(DamageInfo info)
        {
            if (IsDead || invulnerable > 0f || !controller.ControlEnabled) return false;
            Hearts = Mathf.Max(0, Hearts - info.amount);
            HeartsChanged?.Invoke(Hearts, maxHearts);
            CameraController.Shake(0.25f, 0.25f);
            Fx.Burst((Vector2)transform.position + Vector2.up * 0.8f, new Color(1f, 0.3f, 0.3f), 10, 4f, 0.4f);

            if (Hearts <= 0) { Die(); return true; }

            AudioManager.Play(Sfx.PlayerHurt);
            float dir = Mathf.Sign(transform.position.x - info.source.x);
            if (Mathf.Abs(transform.position.x - info.source.x) < 0.05f) dir = -controller.Facing;
            controller.Knockback(new Vector2(dir * knockback.x, knockback.y), 0.25f);
            invulnerable = invulnerabilityTime;
            return true;
        }

        /// <summary>Falling into a pit costs a heart and returns the player to the last checkpoint.</summary>
        public void FellIntoPit()
        {
            if (IsDead) return;
            Hearts = Mathf.Max(0, Hearts - 1);
            HeartsChanged?.Invoke(Hearts, maxHearts);
            if (Hearts <= 0) { Die(); return; }
            AudioManager.Play(Sfx.PlayerHurt);
            controller.SetControl(false);
            SceneLoader.FadeAround(() =>
            {
                if (LevelManager.Instance != null) controller.Teleport(LevelManager.Instance.RespawnPoint);
                if (CameraController.Current != null) CameraController.Current.SnapToTarget();
                controller.SetControl(true);
                invulnerable = invulnerabilityTime;
            });
        }

        public void Heal(int amount)
        {
            if (IsDead) return;
            Hearts = Mathf.Min(maxHearts, Hearts + amount);
            HeartsChanged?.Invoke(Hearts, maxHearts);
        }

        void Die()
        {
            if (IsDead) return;
            IsDead = true;
            AudioManager.Play(Sfx.PlayerDie);
            GameSession.Instance.AddDeath();
            StartCoroutine(DeathSequence());
        }

        IEnumerator DeathSequence()
        {
            controller.SetControl(false);
            var rb = controller.Body;
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f;
            // brief freeze so the hit reads, then a hop and fall off-screen
            yield return new WaitForSeconds(0.35f);
            bodyCollider.enabled = false;
            rb.gravityScale = 3f;
            rb.linearVelocity = new Vector2(-controller.Facing * 2f, 11f);
            float t = 0f;
            while (t < 1.3f)
            {
                t += Time.deltaTime;
                if (controller.visual != null) controller.visual.Rotate(0f, 0f, 360f * Time.deltaTime * -controller.Facing);
                if (spriteRenderer != null) spriteRenderer.color = Color.Lerp(Color.white, new Color(1f, 0.3f, 0.3f, 0f), t / 1.3f);
                yield return null;
            }
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f;
            if (LevelManager.Instance != null) LevelManager.Instance.OnPlayerDied();
        }

        public void Revive(Vector2 position)
        {
            StopAllCoroutines();
            IsDead = false;
            Hearts = maxHearts;
            HeartsChanged?.Invoke(Hearts, maxHearts);
            bodyCollider.enabled = true;
            controller.Body.gravityScale = controller.gravityScale;
            if (controller.visual != null) controller.visual.localRotation = Quaternion.identity;
            if (spriteRenderer != null) { spriteRenderer.color = Color.white; spriteRenderer.enabled = true; }
            controller.Teleport(position);
            controller.SetControl(true);
            invulnerable = 1f;
        }

        void Update()
        {
            if (invulnerable > 0f)
            {
                invulnerable -= Time.deltaTime;
                if (spriteRenderer != null) spriteRenderer.enabled = invulnerable <= 0f || Mathf.Repeat(invulnerable, 0.12f) > 0.05f;
            }
        }
    }
}
