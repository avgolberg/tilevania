namespace TileVania
{
    /// <summary>Restores one heart. Stays in the world if the player is already at full health.</summary>
    public class HeartPickup : Pickup
    {
        public int amount = 1;

        protected override bool CanCollect(PlayerController player)
        {
            var h = player.GetComponent<PlayerHealth>();
            return h != null && h.Hearts < h.maxHearts;
        }

        protected override void OnCollected(PlayerController player)
        {
            if (player != null) player.GetComponent<PlayerHealth>().Heal(amount);
        }
    }
}
