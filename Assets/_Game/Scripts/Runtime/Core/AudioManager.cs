using System.Collections;
using UnityEngine;

namespace TileVania
{
    /// <summary>
    /// Persistent 2D audio: one cross-fading music channel and pooled one-shot SFX.
    /// Music and SFX volumes are independent and saved in PlayerPrefs.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        const string MusicKey = "vol_music", SfxKey = "vol_sfx";
        const int SfxVoices = 12;

        static AudioManager instance;

        public static AudioManager Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("[AudioManager]");
                    instance = go.AddComponent<AudioManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        SfxLibrary library;
        AudioSource[] music;
        AudioSource[] voices;
        int activeMusic, nextVoice;
        Coroutine fade;

        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(MusicKey, 0.6f);
            set { PlayerPrefs.SetFloat(MusicKey, value); if (instance) instance.ApplyMusicVolume(); }
        }

        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat(SfxKey, 0.8f);
            set => PlayerPrefs.SetFloat(SfxKey, value);
        }

        void Awake()
        {
            library = Resources.Load<SfxLibrary>("SfxLibrary");
            music = new AudioSource[2];
            for (int i = 0; i < 2; i++)
            {
                music[i] = gameObject.AddComponent<AudioSource>();
                music[i].loop = true;
                music[i].playOnAwake = false;
                music[i].spatialBlend = 0f;
            }
            voices = new AudioSource[SfxVoices];
            for (int i = 0; i < SfxVoices; i++)
            {
                voices[i] = gameObject.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
                voices[i].spatialBlend = 0f;
            }
        }

        public static void Play(Sfx id) => Instance.PlayInternal(id, 1f);
        public static void Play(Sfx id, float volumeScale) => Instance.PlayInternal(id, volumeScale);

        void PlayInternal(Sfx id, float volumeScale)
        {
            if (library == null || id == Sfx.None) return;
            var entry = library.Find(id);
            if (entry == null || entry.clips == null || entry.clips.Length == 0) return;
            var clip = entry.clips[Random.Range(0, entry.clips.Length)];
            if (clip == null) return;
            var src = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;
            src.pitch = 1f + Random.Range(-entry.pitchVariance, entry.pitchVariance);
            src.PlayOneShot(clip, entry.volume * volumeScale * SfxVolume);
        }

        public static void PlayMusic(AudioClip clip, float fadeTime = 1f)
        {
            var am = Instance;
            if (clip == null) { am.StopMusic(fadeTime); return; }
            if (am.music[am.activeMusic].clip == clip && am.music[am.activeMusic].isPlaying) return;
            if (am.fade != null) am.StopCoroutine(am.fade);
            am.fade = am.StartCoroutine(am.CrossFade(clip, fadeTime));
        }

        void StopMusic(float fadeTime)
        {
            if (fade != null) StopCoroutine(fade);
            fade = StartCoroutine(CrossFade(null, fadeTime));
        }

        IEnumerator CrossFade(AudioClip clip, float time)
        {
            var from = music[activeMusic];
            activeMusic = 1 - activeMusic;
            var to = music[activeMusic];
            float fromStart = from.volume;
            if (clip != null) { to.clip = clip; to.volume = 0f; to.Play(); }
            for (float t = 0; t < time; t += Time.unscaledDeltaTime)
            {
                float k = t / time;
                from.volume = fromStart * (1f - k);
                if (clip != null) to.volume = MusicVolume * k;
                yield return null;
            }
            from.Stop();
            from.clip = null;
            if (clip != null) to.volume = MusicVolume;
            fade = null;
        }

        void ApplyMusicVolume()
        {
            if (fade == null && music[activeMusic].isPlaying) music[activeMusic].volume = MusicVolume;
        }

        void OnDestroy() { if (instance == this) instance = null; }
    }
}
