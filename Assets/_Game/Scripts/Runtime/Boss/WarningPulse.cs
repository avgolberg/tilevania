using UnityEngine;

namespace TileVania
{
    /// <summary>Pulses a warning marker's alpha so danger zones are easy to read.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class WarningPulse : MonoBehaviour
    {
        SpriteRenderer sr;
        void Awake() => sr = GetComponent<SpriteRenderer>();

        void Update()
        {
            var c = sr.color;
            c.a = 0.35f + 0.55f * Mathf.PingPong(Time.time * 4f, 1f);
            sr.color = c;
        }
    }
}
