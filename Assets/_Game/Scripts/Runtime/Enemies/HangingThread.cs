using UnityEngine;

namespace TileVania
{
    /// <summary>Draws a silk thread from an anchor point down to a bobbing spider.</summary>
    public class HangingThread : MonoBehaviour
    {
        public Transform spider;
        public SpriteRenderer thread;
        public float anchorY;

        void Start() => anchorY = transform.position.y;

        void LateUpdate()
        {
            if (spider == null || thread == null) { if (thread) thread.enabled = false; return; }
            float top = anchorY, bottom = spider.position.y + 0.5f;
            float len = Mathf.Max(0.01f, top - bottom);
            thread.transform.position = new Vector3(spider.position.x, bottom + len * 0.5f, 0f);
            thread.transform.localScale = new Vector3(1f, len * 16f, 1f);
        }
    }
}
