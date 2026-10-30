using System.Collections;
using UnityEngine;

namespace TileVania
{
    /// <summary>
    /// A solid gate that opens when the player touches it while holding the matching key.
    /// The solid collider blocks the player and projectiles; a wider trigger detects the player.
    /// </summary>
    public class LockedGate : MonoBehaviour
    {
        public KeyType requiredKey = KeyType.Gold;
        public Collider2D solidCollider;
        public Transform visual;
        public string lockedMessage = "Locked. Find the Gold Key.";

        bool open;
        float messageCooldown;

        void Update() => messageCooldown -= Time.deltaTime;

        void OnTriggerStay2D(Collider2D other)
        {
            if (open) return;
            var player = other.attachedRigidbody != null ? other.attachedRigidbody.GetComponent<PlayerController>() : null;
            if (player == null) return;
            var session = GameSession.Instance;
            if (session.HasKey(requiredKey))
            {
                session.UseKey(requiredKey);
                StartCoroutine(Open());
            }
            else if (messageCooldown <= 0f)
            {
                messageCooldown = 2.5f;
                AudioManager.Play(Sfx.Locked);
                HUD.ShowMessage(lockedMessage);
            }
        }

        IEnumerator Open()
        {
            open = true;
            AudioManager.Play(Sfx.Unlock);
            CameraController.Shake(0.15f, 0.4f);
            if (solidCollider != null) solidCollider.enabled = false;
            Fx.Burst(transform.position + Vector3.up, new Color(1f, 0.85f, 0.4f), 16, 4f, 0.6f);
            if (visual == null) yield break;
            Vector3 start = visual.localPosition;
            var srs = visual.GetComponentsInChildren<SpriteRenderer>();
            for (float t = 0; t < 1f; t += Time.deltaTime)
            {
                visual.localPosition = start + Vector3.up * (t * 2.2f);
                foreach (var sr in srs) { var c = sr.color; c.a = 1f - t; sr.color = c; }
                yield return null;
            }
            visual.gameObject.SetActive(false);
        }
    }
}
