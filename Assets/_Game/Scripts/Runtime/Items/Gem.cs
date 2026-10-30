namespace TileVania
{
    public class Gem : Pickup
    {
        public int value = 1;

        protected override void OnCollected(PlayerController player) => GameSession.Instance.AddGems(value);
    }
}
