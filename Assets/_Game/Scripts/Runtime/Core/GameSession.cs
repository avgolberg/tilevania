using System;
using System.Collections.Generic;
using UnityEngine;

namespace TileVania
{
    public enum KeyType { Gold, Treasure }

    /// <summary>
    /// Run-wide state that survives scene loads: gems, deaths, play time.
    /// Keys are per level and cleared when a level starts.
    /// Created on demand; never needs to be placed in a scene.
    /// </summary>
    public class GameSession : MonoBehaviour
    {
        static GameSession instance;

        public static GameSession Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("[GameSession]");
                    instance = go.AddComponent<GameSession>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        public static bool Exists => instance != null;

        public int Gems { get; private set; }
        public int Deaths { get; private set; }
        public float PlayTime { get; private set; }
        public bool TimerRunning { get; set; }

        int gemsAtLevelStart;
        readonly HashSet<KeyType> keys = new HashSet<KeyType>();

        public event Action Changed;

        public static void StartNewRun()
        {
            var s = Instance;
            s.Gems = 0; s.Deaths = 0; s.PlayTime = 0f; s.gemsAtLevelStart = 0;
            s.keys.Clear();
            s.Changed?.Invoke();
        }

        public void BeginLevel()
        {
            gemsAtLevelStart = Gems;
            keys.Clear();
            TimerRunning = true;
            Changed?.Invoke();
        }

        /// <summary>Restarting a level from the pause menu discards the gems found in it.</summary>
        public void RevertLevel()
        {
            Gems = gemsAtLevelStart;
            keys.Clear();
            Changed?.Invoke();
        }

        public void AddGems(int amount) { Gems += amount; Changed?.Invoke(); }
        public void AddDeath() { Deaths++; Changed?.Invoke(); }
        public void AddKey(KeyType key) { keys.Add(key); Changed?.Invoke(); }
        public bool HasKey(KeyType key) => keys.Contains(key);
        public void UseKey(KeyType key) { keys.Remove(key); Changed?.Invoke(); }

        void Update()
        {
            if (TimerRunning && Time.timeScale > 0f) PlayTime += Time.unscaledDeltaTime;
        }

        public static string FormatTime(float seconds)
        {
            int s = Mathf.FloorToInt(seconds);
            return $"{s / 60:00}:{s % 60:00}";
        }

        void OnDestroy() { if (instance == this) instance = null; }
    }
}
