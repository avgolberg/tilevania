using System.Collections;
using UnityEngine;

namespace TileVania
{
    /// <summary>
    /// Portal to the next scene. Only the player can trigger it (projectiles never reach this layer),
    /// and it fires exactly once. Plays its opening animation when the player approaches.
    /// </summary>
    public class LevelExit : MonoBehaviour
    {
        public SpriteFrameAnimator animator;
        public Sprite[] openFrames;
        public Sprite[] loopFrames;
        public float openRadius = 6f;

        bool opened, used;
        Transform player;

        void Start()
        {
            var pc = FindFirstObjectByType<PlayerController>();
            if (pc != null) player = pc.transform;
            if (animator != null && openFrames != null && openFrames.Length > 0)
                animator.Play(new[] { openFrames[0] }, 1f, true);
        }

        void Update()
        {
            if (opened || player == null || animator == null) return;
            if (Vector2.Distance(player.position, transform.position) < openRadius)
            {
                opened = true;
                StartCoroutine(OpenSequence());
            }
        }

        IEnumerator OpenSequence()
        {
            AudioManager.Play(Sfx.Portal, 0.6f);
            animator.Play(openFrames, 18f, false);
            yield return new WaitForSeconds(openFrames.Length / 18f);
            animator.Play(loopFrames, 8f, true);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (used) return;
            var pc = other.attachedRigidbody != null ? other.attachedRigidbody.GetComponent<PlayerController>() : null;
            if (pc == null || pc.GetComponent<PlayerHealth>().IsDead) return;
            used = true;
            StartCoroutine(Enter(pc));
        }

        IEnumerator Enter(PlayerController pc)
        {
            pc.SetControl(false);
            pc.Body.linearVelocity = Vector2.zero;
            AudioManager.Play(Sfx.Portal);
            Vector3 start = pc.transform.position;
            Vector3 center = transform.position + Vector3.up * 0.2f;
            for (float t = 0; t < 0.6f; t += Time.deltaTime)
            {
                float k = t / 0.6f;
                pc.Body.position = Vector3.Lerp(start, center, k);
                if (pc.visual != null) pc.visual.localScale = new Vector3(pc.Facing, 1f, 1f) * (1f - k);
                yield return null;
            }
            if (pc.visual != null) pc.visual.gameObject.SetActive(false);
            if (LevelManager.Instance != null) LevelManager.Instance.CompleteLevel();
        }
    }
}
