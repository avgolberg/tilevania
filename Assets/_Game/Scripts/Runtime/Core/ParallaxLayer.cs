using UnityEngine;

namespace TileVania
{
    /// <summary>
    /// Moves a background layer with a fraction of the camera movement.
    /// factor 1 = fixed to camera (infinitely far), 0 = static with the world.
    /// </summary>
    public class ParallaxLayer : MonoBehaviour
    {
        public Vector2 factor = new Vector2(0.8f, 0.9f);

        Transform cam;
        Vector3 startPos, camStart;

        void Start()
        {
            cam = Camera.main != null ? Camera.main.transform : null;
            startPos = transform.position;
            if (cam != null) camStart = cam.position;
        }

        void LateUpdate()
        {
            if (cam == null) return;
            var d = cam.position - camStart;
            transform.position = new Vector3(startPos.x + d.x * factor.x, startPos.y + d.y * factor.y, startPos.z);
        }
    }
}
