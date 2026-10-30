using System;
using UnityEngine;

namespace TileVania
{
    public enum Sfx
    {
        None, Jump, Land, Shoot, Gem, Key, Chest, Unlock, Locked, EnemyHit, EnemyDie, PlayerHurt, PlayerDie,
        Checkpoint, Portal, UIClick, UISelect, BossWarning, BossHit, EnemyShoot, RockImpact, BossDie, Victory,
        ProjectileHit, Heal
    }

    /// <summary>Maps sound events to clips so audio can be swapped without touching code.</summary>
    [CreateAssetMenu(menuName = "TileVania/Sfx Library")]
    public class SfxLibrary : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public Sfx id;
            public AudioClip[] clips;
            [Range(0f, 1f)] public float volume = 0.8f;
            [Range(0f, 0.5f)] public float pitchVariance = 0.05f;
        }

        public Entry[] entries = Array.Empty<Entry>();

        public Entry Find(Sfx id)
        {
            foreach (var e in entries) if (e.id == id) return e;
            return null;
        }
    }
}
