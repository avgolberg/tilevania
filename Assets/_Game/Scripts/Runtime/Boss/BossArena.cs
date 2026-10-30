using System.Collections;
using UnityEngine;

namespace TileVania
{
    /// <summary>
    /// Starts the boss fight when the player walks in and seals the entrance behind them.
    /// If the player dies and retries, the fight resets and the entrance reopens.
    /// </summary>
    public class BossArena : MonoBehaviour
    {
        public DragonBoss boss;
        public Collider2D doorCollider;
        public Transform doorVisual;
        public float doorClosedY;
        public float doorOpenY = 3f;

        bool triggered;

        void Start()
        {
            SetDoor(false, true);
            if (LevelManager.Instance != null) LevelManager.Instance.PlayerRespawned += OnRespawn;
        }

        void OnDestroy()
        {
            if (LevelManager.Instance != null) LevelManager.Instance.PlayerRespawned -= OnRespawn;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (triggered || boss == null || boss.IsDead) return;
            var pc = other.attachedRigidbody != null ? other.attachedRigidbody.GetComponent<PlayerController>() : null;
            if (pc == null) return;
            triggered = true;
            SetDoor(true, false);
            boss.StartFight();
        }

        void OnRespawn()
        {
            if (boss == null || boss.IsDead) return;
            boss.ResetFight();
            triggered = false;
            SetDoor(false, true);
            if (LevelManager.Instance != null) AudioManager.PlayMusic(LevelManager.Instance.music);
        }

        void SetDoor(bool closed, bool instant)
        {
            if (doorCollider) doorCollider.enabled = closed;
            if (doorVisual == null) return;
            StopAllCoroutines();
            if (instant) doorVisual.localPosition = new Vector3(0f, closed ? doorClosedY : doorOpenY, 0f);
            else StartCoroutine(SlideDoor(closed ? doorClosedY : doorOpenY));
        }

        IEnumerator SlideDoor(float y)
        {
            AudioManager.Play(Sfx.Unlock);
            Vector3 from = doorVisual.localPosition, to = new Vector3(0f, y, 0f);
            for (float t = 0; t < 1f; t += Time.deltaTime / 0.35f)
            {
                doorVisual.localPosition = Vector3.Lerp(from, to, t);
                yield return null;
            }
            doorVisual.localPosition = to;
            CameraController.Shake(0.2f, 0.25f);
        }
    }
}
