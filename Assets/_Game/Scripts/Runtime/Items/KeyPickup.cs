namespace TileVania
{
    public class KeyPickup : Pickup
    {
        public KeyType key = KeyType.Gold;
        public string message = "Found the Gold Key!";

        protected override void OnCollected(PlayerController player)
        {
            GameSession.Instance.AddKey(key);
            HUD.ShowMessage(message);
        }
    }
}
