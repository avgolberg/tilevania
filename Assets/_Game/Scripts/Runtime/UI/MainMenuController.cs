using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TileVania
{
    public class MainMenuController : MonoBehaviour
    {
        public AudioClip music;
        public Button playButton, settingsButton, creditsButton, quitButton;
        public GameObject mainPanel, settingsPanel, creditsPanel;
        public Button settingsBackButton, creditsBackButton;

        void Awake()
        {
            Time.timeScale = 1f;
            playButton.onClick.AddListener(Play);
            settingsButton.onClick.AddListener(() => Show(settingsPanel, settingsBackButton));
            creditsButton.onClick.AddListener(() => Show(creditsPanel, creditsBackButton));
            settingsBackButton.onClick.AddListener(() => Show(mainPanel, settingsButton));
            creditsBackButton.onClick.AddListener(() => Show(mainPanel, creditsButton));
            quitButton.onClick.AddListener(Quit);
        }

        void Start()
        {
            AudioManager.PlayMusic(music);
            Show(mainPanel, playButton);
        }

        void Show(GameObject panel, Button select)
        {
            mainPanel.SetActive(panel == mainPanel);
            settingsPanel.SetActive(panel == settingsPanel);
            creditsPanel.SetActive(panel == creditsPanel);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(select.gameObject);
        }

        void Update()
        {
            // Keep keyboard navigation alive if the mouse clicked empty space.
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
            {
                var b = mainPanel.activeSelf ? playButton : settingsPanel.activeSelf ? settingsBackButton : creditsBackButton;
                EventSystem.current.SetSelectedGameObject(b.gameObject);
            }
        }

        void Play()
        {
            GameSession.StartNewRun();
            SceneLoader.Load(SceneLoader.Level1);
        }

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
