using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace TileVania
{
    /// <summary>Touch to set the respawn point. Lights a flame and restores health.</summary>
    public class Checkpoint : MonoBehaviour
    {
        public GameObject activeVisual;
        public Light2D activeLight;
        public Vector2 respawnOffset = new Vector2(0f, 0.1f);

        public bool IsActive { get; private set; }
        public Vector2 RespawnPosition => (Vector2)transform.position + respawnOffset;

        void Awake() => SetVisual(false);

        void OnTriggerEnter2D(Collider2D other)
        {
            if (IsActive) return;
            var player = other.attachedRigidbody != null ? other.attachedRigidbody.GetComponent<PlayerController>() : null;
            if (player == null || LevelManager.Instance == null) return;
            LevelManager.Instance.SetCheckpoint(this);
            player.GetComponent<PlayerHealth>().Heal(99);
            AudioManager.Play(Sfx.Checkpoint);
            Fx.Burst(transform.position + Vector3.up * 1.2f, new Color(1f, 0.7f, 0.25f), 14, 3f, 0.6f, -2f);
            HUD.ShowMessage("Checkpoint");
        }

        public void SetActive(bool value)
        {
            IsActive = value;
            SetVisual(value);
        }

        void SetVisual(bool on)
        {
            if (activeVisual != null) activeVisual.SetActive(on);
            if (activeLight != null) activeLight.enabled = on;
        }
    }
}
