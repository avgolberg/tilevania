using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TileVania
{
    /// <summary>
    /// Per-scene coordinator: level start (music, title), checkpoints, death and retry,
    /// restart, completion and victory. Progression uses explicit scene names, never build-index wrapping.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        public string levelTitle = "Level";
        public string levelSubtitle = "";
        public AudioClip music;
        public string nextScene = SceneLoader.MainMenu;
        public PlayerController player;
        public Transform spawnPoint;
        public Material spriteMaterial;

        Checkpoint checkpoint;

        public event Action PlayerRespawned;

        public Vector2 RespawnPoint =>
            checkpoint != null ? checkpoint.RespawnPosition : (spawnPoint != null ? (Vector2)spawnPoint.position : Vector2.zero);

        void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;
            Layers.ApplyCollisionMatrix();
            Fx.RegisterMaterial(spriteMaterial);
        }

        void Start()
        {
            GameSession.Instance.BeginLevel();
            AudioManager.PlayMusic(music);
            HUD.ShowTitle(levelTitle, levelSubtitle);
        }

        public void SetCheckpoint(Checkpoint cp)
        {
            if (checkpoint != null && checkpoint != cp) checkpoint.SetActive(false);
            checkpoint = cp;
            cp.SetActive(true);
        }

        public void OnPlayerDied() => GameMenus.ShowDeath();

        public void Retry()
        {
            GameMenus.HideAll();
            var health = player.GetComponent<PlayerHealth>();
            health.Revive(RespawnPoint);
            if (CameraController.Current != null) CameraController.Current.SnapToTarget();
            PlayerRespawned?.Invoke();
        }

        public void RestartLevel()
        {
            GameSession.Instance.RevertLevel();
            SceneLoader.Load(SceneManager.GetActiveScene().name);
        }

        public void CompleteLevel() => SceneLoader.Load(nextScene);

        public void Victory()
        {
            GameSession.Instance.TimerRunning = false;
            AudioManager.Play(Sfx.Victory);
            if (player != null) player.SetControl(false);
            GameMenus.ShowVictory();
        }

        public void QuitToMenu() => SceneLoader.Load(SceneLoader.MainMenu);

        void OnDestroy() { if (Instance == this) Instance = null; }
    }
}
