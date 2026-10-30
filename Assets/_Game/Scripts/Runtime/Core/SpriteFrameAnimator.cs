using UnityEngine;

namespace TileVania
{
    /// <summary>Simple looping (or one-shot) flipbook for props, enemies and pickups.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteFrameAnimator : MonoBehaviour
    {
        public Sprite[] frames;
        public float fps = 8f;
        public bool loop = true;
        public bool randomStartFrame = true;

        SpriteRenderer sr;
        float time;

        public bool Finished => !loop && frames != null && time * fps >= frames.Length;

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            if (randomStartFrame && frames != null && frames.Length > 0) time = Random.Range(0f, frames.Length / fps);
        }

        public void Play(Sprite[] newFrames, float newFps, bool newLoop)
        {
            frames = newFrames; fps = newFps; loop = newLoop; time = 0f;
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            if (frames != null && frames.Length > 0) sr.sprite = frames[0];
        }

        void Update()
        {
            if (frames == null || frames.Length == 0 || fps <= 0f) return;
            time += Time.deltaTime;
            int i = Mathf.FloorToInt(time * fps);
            i = loop ? i % frames.Length : Mathf.Min(i, frames.Length - 1);
            sr.sprite = frames[i];
        }
    }
}
