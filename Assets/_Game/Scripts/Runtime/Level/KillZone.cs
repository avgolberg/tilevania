using UnityEngine;

namespace TileVania
{
    /// <summary>Bottomless pits: costs a heart and returns the player to the last checkpoint.</summary>
    public class KillZone : MonoBehaviour
    {
        void OnTriggerEnter2D(Collider2D other)
        {
            var health = other.attachedRigidbody != null ? other.attachedRigidbody.GetComponent<PlayerHealth>() : null;
            if (health != null) health.FellIntoPit();
        }
    }
}
