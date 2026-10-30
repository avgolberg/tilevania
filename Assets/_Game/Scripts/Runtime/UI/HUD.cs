using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TileVania
{
    /// <summary>Hearts, gem counter, held keys, level title card, message toasts and the boss health bar.</summary>
    public class HUD : MonoBehaviour
    {
        static HUD instance;

        public Image[] hearts;
        public Sprite heartFull, heartEmpty;
        public TMP_Text gemText;
        public GameObject goldKeyIcon, treasureKeyIcon;
        public CanvasGroup titleGroup;
        public TMP_Text titleText, subtitleText;
        public CanvasGroup messageGroup;
        public TMP_Text messageText;
        public GameObject bossBarRoot;
        public Image bossBarFill;
        public TMP_Text bossNameText;

        PlayerHealth playerHealth;
        Coroutine messageRoutine, titleRoutine;

        void Awake()
        {
            instance = this;
            if (titleGroup) titleGroup.alpha = 0f;
            if (messageGroup) messageGroup.alpha = 0f;
            if (bossBarRoot) bossBarRoot.SetActive(false);
        }

        void Start()
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.HeartsChanged += OnHeartsChanged;
                OnHeartsChanged(playerHealth.Hearts, playerHealth.maxHearts);
            }
            GameSession.Instance.Changed += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (playerHealth != null) playerHealth.HeartsChanged -= OnHeartsChanged;
            if (GameSession.Exists) GameSession.Instance.Changed -= Refresh;
            if (instance == this) instance = null;
        }

        void OnHeartsChanged(int current, int max)
        {
            for (int i = 0; i < hearts.Length; i++)
            {
                hearts[i].gameObject.SetActive(i < max);
                hearts[i].sprite = i < current ? heartFull : heartEmpty;
            }
        }

        void Refresh()
        {
            var s = GameSession.Instance;
            if (gemText) gemText.text = s.Gems.ToString();
            if (goldKeyIcon) goldKeyIcon.SetActive(s.HasKey(KeyType.Gold));
            if (treasureKeyIcon) treasureKeyIcon.SetActive(s.HasKey(KeyType.Treasure));
        }

        public static void ShowMessage(string text)
        {
            if (instance == null || instance.messageText == null) return;
            if (instance.messageRoutine != null) instance.StopCoroutine(instance.messageRoutine);
            instance.messageText.text = text;
            instance.messageRoutine = instance.StartCoroutine(instance.FadeGroup(instance.messageGroup, 2.2f));
        }

        public static void ShowTitle(string title, string subtitle)
        {
            if (instance == null || instance.titleText == null) return;
            instance.titleText.text = title;
            if (instance.subtitleText) instance.subtitleText.text = subtitle;
            if (instance.titleRoutine != null) instance.StopCoroutine(instance.titleRoutine);
            instance.titleRoutine = instance.StartCoroutine(instance.FadeGroup(instance.titleGroup, 2.5f));
        }

        public static void ShowBossBar(string bossName)
        {
            if (instance == null || instance.bossBarRoot == null) return;
            instance.bossBarRoot.SetActive(true);
            if (instance.bossNameText) instance.bossNameText.text = bossName;
            SetBossHealth(1f);
        }

        public static void SetBossHealth(float normalized)
        {
            if (instance == null || instance.bossBarFill == null) return;
            instance.bossBarFill.fillAmount = Mathf.Clamp01(normalized);
        }

        public static void HideBossBar()
        {
            if (instance != null && instance.bossBarRoot != null) instance.bossBarRoot.SetActive(false);
        }

        IEnumerator FadeGroup(CanvasGroup g, float hold)
        {
            for (float t = 0; t < 0.3f; t += Time.unscaledDeltaTime) { g.alpha = t / 0.3f; yield return null; }
            g.alpha = 1f;
            yield return new WaitForSecondsRealtime(hold);
            for (float t = 0; t < 0.6f; t += Time.unscaledDeltaTime) { g.alpha = 1f - t / 0.6f; yield return null; }
            g.alpha = 0f;
        }
    }
}
