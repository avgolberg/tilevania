using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace TileVania
{
    /// <summary>Subtle torch/fire flicker for a 2D light.</summary>
    [RequireComponent(typeof(Light2D))]
    public class LightFlicker : MonoBehaviour
    {
        public float amount = 0.15f;
        public float speed = 6f;

        Light2D lightSource;
        float baseIntensity, seed;

        void Awake()
        {
            lightSource = GetComponent<Light2D>();
            baseIntensity = lightSource.intensity;
            seed = Random.Range(0f, 100f);
        }

        void Update()
        {
            float n = Mathf.PerlinNoise(seed, Time.time * speed) - 0.5f;
            lightSource.intensity = baseIntensity * (1f + n * 2f * amount);
        }
    }
}
