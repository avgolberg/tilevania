using UnityEngine;

namespace TileVania
{
    public struct DamageInfo
    {
        public int amount;
        public Vector2 source;

        public DamageInfo(int amount, Vector2 source)
        {
            this.amount = amount;
            this.source = source;
        }
    }

    /// <summary>Anything that projectiles or hazards can hurt.</summary>
    public interface IDamageable
    {
        /// <returns>true if the hit was accepted (e.g. not during invulnerability).</returns>
        bool TakeDamage(DamageInfo info);
    }
}
