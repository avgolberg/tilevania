using UnityEngine;

namespace TileVania
{
    /// <summary>
    /// Smooth platformer camera: follows the target with look-ahead, stays inside level bounds,
    /// supports screen shake and snaps to the pixel grid to keep pixel art crisp.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        public Transform target;
        public Rect bounds = new Rect(0, 0, 100, 30);
        public float smoothTime = 0.15f;
        public float lookAhead = 2f;
        public float verticalOffset = 1.5f;
        public int pixelsPerUnit = 16;

        static CameraController current;
        Camera cam;
        Vector3 velocity;
        float lookAheadX;
        float shakeTime, shakeDuration, shakeAmount;

        public static CameraController Current => current;

        void Awake()
        {
            cam = GetComponent<Camera>();
            current = this;
        }

        void Start() => SnapToTarget();

        public void SnapToTarget()
        {
            if (target == null) return;
            basePos = Clamp(new Vector3(target.position.x, target.position.y + verticalOffset, -10f));
            transform.position = basePos;
            velocity = Vector3.zero;
        }

        public static void Shake(float amount, float duration)
        {
            if (current == null) return;
            if (amount < current.shakeAmount && current.shakeTime > 0f) return;
            current.shakeAmount = amount;
            current.shakeDuration = current.shakeTime = duration;
        }

        void LateUpdate()
        {
            if (target == null) return;
            var rb = target.GetComponent<Rigidbody2D>();
            float vx = rb != null ? rb.linearVelocity.x : 0f;
            float desiredLook = Mathf.Abs(vx) > 0.5f ? Mathf.Sign(vx) * lookAhead : lookAheadX;
            lookAheadX = Mathf.MoveTowards(lookAheadX, desiredLook, Time.deltaTime * 4f);

            var goal = new Vector3(target.position.x + lookAheadX, target.position.y + verticalOffset, -10f);
            var pos = Vector3.SmoothDamp(basePos, goal, ref velocity, smoothTime);
            basePos = Clamp(pos);

            var final = basePos;
            if (shakeTime > 0f)
            {
                shakeTime -= Time.unscaledDeltaTime;
                float k = shakeTime / shakeDuration;
                final += (Vector3)(Random.insideUnitCircle * shakeAmount * k);
            }
            transform.position = PixelSnap(final);
        }

        Vector3 basePos;

        void OnEnable() { basePos = transform.position; }

        Vector3 Clamp(Vector3 p)
        {
            if (cam == null) cam = GetComponent<Camera>();
            float h = cam.orthographicSize, w = h * cam.aspect;
            float minX = bounds.xMin + w, maxX = bounds.xMax - w;
            float minY = bounds.yMin + h, maxY = bounds.yMax - h;
            p.x = minX > maxX ? bounds.center.x : Mathf.Clamp(p.x, minX, maxX);
            p.y = minY > maxY ? bounds.center.y : Mathf.Clamp(p.y, minY, maxY);
            return p;
        }

        Vector3 PixelSnap(Vector3 p)
        {
            float screenPixelsPerUnit = Screen.height / (cam.orthographicSize * 2f);
            float unit = 1f / Mathf.Max(pixelsPerUnit, screenPixelsPerUnit);
            p.x = Mathf.Round(p.x / unit) * unit;
            p.y = Mathf.Round(p.y / unit) * unit;
            return p;
        }

        void OnDestroy() { if (current == this) current = null; }
    }
}
