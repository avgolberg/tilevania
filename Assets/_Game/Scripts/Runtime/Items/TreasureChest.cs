using System;
using System.Collections;
using UnityEngine;

namespace TileVania
{
    /// <summary>Opens when the player touches it and showers out gems (and optionally a heal).</summary>
    public class TreasureChest : MonoBehaviour
    {
        public Sprite[] openFrames;
        public SpriteRenderer spriteRenderer;
        public Gem gemPrefab;
        public int gemCount = 5;
        public int healAmount = 1;
        public bool isFinalTreasure;
        public UnityEngine.Rendering.Universal.Light2D glow;

        bool opened;

        public event Action Opened;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (opened) return;
            var player = other.attachedRigidbody != null ? other.attachedRigidbody.GetComponent<PlayerController>() : null;
            if (player == null) return;
            if (player.GetComponent<PlayerHealth>().IsDead) return;
            opened = true;
            StartCoroutine(Open(player));
        }

        IEnumerator Open(PlayerController player)
        {
            AudioManager.Play(Sfx.Chest);
            if (openFrames != null)
                foreach (var f in openFrames)
                {
                    spriteRenderer.sprite = f;
                    yield return new WaitForSeconds(0.08f);
                }
            Fx.Burst(transform.position + Vector3.up * 0.6f, new Color(1f, 0.85f, 0.3f), 20, 5f, 0.6f);
            if (glow != null) glow.intensity *= 2f;

            if (healAmount > 0) player.GetComponent<PlayerHealth>().Heal(healAmount);
            for (int i = 0; i < gemCount && gemPrefab != null; i++)
            {
                var g = Instantiate(gemPrefab, transform.position + Vector3.up * 0.6f, Quaternion.identity);
                g.magnetToPlayer = true;
                yield return new WaitForSeconds(0.06f);
            }
            if (isFinalTreasure)
            {
                HUD.ShowMessage("The Guardian's treasure is yours!");
                yield return new WaitForSeconds(1.5f);
            }
            Opened?.Invoke();
            if (isFinalTreasure && LevelManager.Instance != null) LevelManager.Instance.Victory();
        }
    }
}
