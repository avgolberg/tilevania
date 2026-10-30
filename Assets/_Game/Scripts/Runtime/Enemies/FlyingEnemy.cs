using UnityEngine;

namespace TileVania
{
    /// <summary>Flies back and forth along a horizontal path with a wavy vertical motion.</summary>
    public class FlyingEnemy : MonoBehaviour
    {
        public float distance = 4f;
        public float speed = 2.2f;
        public float waveHeight = 0.6f;
        public float waveFrequency = 1.2f;
        public SpriteRenderer spriteRenderer;
        public bool spriteFacesRight = false;

        Vector3 origin;
        float t;

        void Start()
        {
            origin = transform.position;
            t = Random.Range(0f, 10f);
        }

        void Update()
        {
            t += Time.deltaTime;
            float period = distance * 2f / speed;
            float phase = Mathf.PingPong(t / period * 2f, 1f);
            float x = Mathf.Lerp(-distance * 0.5f, distance * 0.5f, Mathf.SmoothStep(0f, 1f, phase));
            float y = Mathf.Sin(t * waveFrequency * Mathf.PI * 2f) * waveHeight;
            var next = origin + new Vector3(x, y, 0f);
            if (spriteRenderer != null && Mathf.Abs(next.x - transform.position.x) > 0.0001f)
            {
                bool movingRight = next.x > transform.position.x;
                spriteRenderer.flipX = spriteFacesRight ? !movingRight : movingRight;
            }
            transform.position = next;
        }
    }
}
