using UnityEngine;

namespace TileVania
{
    /// <summary>Sine-wave offset for floating pickups, hovering enemies and hanging spiders.</summary>
    public class Bobbing : MonoBehaviour
    {
        public Vector2 amplitude = new Vector2(0f, 0.15f);
        public float frequency = 1.5f;
        public bool randomPhase = true;

        Vector3 origin;
        float phase;

        void Start()
        {
            origin = transform.localPosition;
            if (randomPhase) phase = Random.Range(0f, Mathf.PI * 2f);
        }

        public void ResetOrigin() => origin = transform.localPosition;

        void Update()
        {
            float s = Mathf.Sin(Time.time * frequency * Mathf.PI * 2f + phase);
            transform.localPosition = origin + new Vector3(amplitude.x * s, amplitude.y * s, 0f);
        }
    }
}
