using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TileVania
{
    /// <summary>Persistent fade-to-black scene transitions. Ignores repeated load requests.</summary>
    public class SceneLoader : MonoBehaviour
    {
        public const string MainMenu = "MainMenu";
        public const string Level1 = "Level1_Forest";
        public const string Level2 = "Level2_Castle";
        public const string Boss = "Level3_TreasureChamber";

        static SceneLoader instance;
        Image fadeImage;
        bool loading;

        public static bool IsLoading => instance != null && instance.loading;

        static SceneLoader Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("[SceneLoader]");
                    instance = go.AddComponent<SceneLoader>();
                    DontDestroyOnLoad(go);
                    instance.BuildOverlay();
                }
                return instance;
            }
        }

        void BuildOverlay()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var imgGo = new GameObject("Fade", typeof(RectTransform));
            imgGo.transform.SetParent(transform, false);
            fadeImage = imgGo.AddComponent<Image>();
            fadeImage.color = new Color(0.03f, 0.02f, 0.06f, 0f);
            fadeImage.raycastTarget = false;
            var rt = fadeImage.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        public static void Load(string scene, float fadeOut = 0.6f)
        {
            if (IsLoading) return;
            Instance.StartCoroutine(Instance.LoadRoutine(scene, fadeOut));
        }

        /// <summary>Fade to black, run an action (e.g. teleport the player), fade back in.</summary>
        public static void FadeAround(Action midpoint, float duration = 0.35f)
        {
            Instance.StartCoroutine(Instance.FadeAroundRoutine(midpoint, duration));
        }

        IEnumerator FadeAroundRoutine(Action midpoint, float d)
        {
            yield return Fade(0f, 1f, d);
            midpoint?.Invoke();
            yield return Fade(1f, 0f, d);
        }

        IEnumerator LoadRoutine(string scene, float fadeOut)
        {
            loading = true;
            fadeImage.raycastTarget = true;
            yield return Fade(fadeImage.color.a, 1f, fadeOut);
            Time.timeScale = 1f;
            var op = SceneManager.LoadSceneAsync(scene);
            while (op != null && !op.isDone) yield return null;
            yield return null;
            loading = false;
            yield return Fade(1f, 0f, 0.5f);
            fadeImage.raycastTarget = false;
        }

        IEnumerator Fade(float from, float to, float time)
        {
            var c = fadeImage.color;
            for (float t = 0; t < time; t += Time.unscaledDeltaTime)
            {
                c.a = Mathf.Lerp(from, to, t / time);
                fadeImage.color = c;
                yield return null;
            }
            c.a = to;
            fadeImage.color = c;
        }

        void OnDestroy() { if (instance == this) instance = null; }
    }
}
