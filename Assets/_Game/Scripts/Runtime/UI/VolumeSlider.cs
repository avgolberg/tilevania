using UnityEngine;
using UnityEngine.UI;

namespace TileVania
{
    /// <summary>Binds a UI slider to the saved music or SFX volume.</summary>
    [RequireComponent(typeof(Slider))]
    public class VolumeSlider : MonoBehaviour
    {
        public enum Channel { Music, Sfx }
        public Channel channel;

        Slider slider;

        void Awake()
        {
            slider = GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.onValueChanged.AddListener(OnChanged);
        }

        void OnEnable() => slider.SetValueWithoutNotify(channel == Channel.Music ? AudioManager.MusicVolume : AudioManager.SfxVolume);

        void OnChanged(float v)
        {
            if (channel == Channel.Music) AudioManager.MusicVolume = v;
            else { AudioManager.SfxVolume = v; AudioManager.Play(Sfx.UIClick); }
        }
    }
}
