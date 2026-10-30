using UnityEngine;

namespace TileVania
{
    /// <summary>
    /// Walks back and forth, turning at walls and ledges detected by raycasts
    /// (not by unrelated trigger events). Pivot is at the feet.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PatrolEnemy : MonoBehaviour
    {
        public float speed = 1.6f;
        public float halfWidth = 0.4f;
        public bool spriteFacesRight = false;
        public Transform visual;
        public int startDirection = -1;

        Rigidbody2D rb;
        int dir;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            dir = startDirection >= 0 ? 1 : -1;
        }

        void FixedUpdate()
        {
            Vector2 pos = rb.position;
            Vector2 ahead = pos + new Vector2(dir * (halfWidth + 0.05f), 0f);
            bool wall = Physics2D.Raycast(pos + new Vector2(0f, 0.5f), Vector2.right * dir, halfWidth + 0.1f, Layers.SolidMask);
            bool ground = Physics2D.Raycast(ahead + Vector2.up * 0.1f, Vector2.down, 0.4f, Layers.GroundMask);
            if (wall || !ground) dir = -dir;

            rb.MovePosition(pos + new Vector2(dir * speed * Time.fixedDeltaTime, 0f));
            if (visual != null)
            {
                float sx = Mathf.Abs(visual.localScale.x) * (spriteFacesRight ? dir : -dir);
                visual.localScale = new Vector3(sx, visual.localScale.y, 1f);
            }
        }
    }
}
