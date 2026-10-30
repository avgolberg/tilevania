using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TileVania.Tests
{
    /// <summary>
    /// End-to-end gameplay checks that load the real scenes and drive the player with simulated input.
    /// Coordinates match the ASCII layouts in Assets/_Game/Levels.
    /// </summary>
    public class GameplayTests
    {
        PlayerController player;

        IEnumerator Load(string scene)
        {
            while (SceneLoader.IsLoading) yield return null; // let transitions from a previous test finish
            GameInput.Simulated = new GameInput.SimulatedInput { Enabled = true };
            SceneManager.LoadScene(scene);
            yield return null;
            yield return null;
            player = Object.FindFirstObjectByType<PlayerController>();
            yield return new WaitForSeconds(0.5f);
        }

        [TearDown]
        public void TearDown()
        {
            GameInput.Simulated = default;
            Time.timeScale = 1f;
        }

        void Move(float x, float y = 0f) => GameInput.Simulated.Move = new Vector2(x, y);

        IEnumerator Jump()
        {
            GameInput.Simulated.JumpPressed = true;
            GameInput.Simulated.JumpHeld = true;
            yield return new WaitForSeconds(0.4f);
            GameInput.Simulated.JumpHeld = false;
        }

        IEnumerator TeleportAndSettle(Vector2 pos)
        {
            player.Teleport(pos);
            yield return new WaitForSeconds(0.4f);
        }

        [UnityTest]
        public IEnumerator Player_SpawnsGrounded_AndRuns()
        {
            yield return Load(SceneLoader.Level1);
            Assert.IsNotNull(player);
            Assert.IsTrue(player.IsGrounded, "player should stand on the ground at spawn");
            float x0 = player.transform.position.x;
            Move(1f);
            yield return new WaitForSeconds(0.6f);
            Move(0f);
            Assert.Greater(player.transform.position.x, x0 + 2f, "player should run right");
            Assert.AreEqual(1, player.Facing);
        }

        [UnityTest]
        public IEnumerator Player_Jumps_AboutThreeTiles()
        {
            yield return Load(SceneLoader.Level1);
            float y0 = player.transform.position.y, maxY = y0;
            GameInput.Simulated.JumpPressed = true;
            GameInput.Simulated.JumpHeld = true;
            for (float t = 0; t < 0.8f; t += Time.deltaTime)
            {
                maxY = Mathf.Max(maxY, player.transform.position.y);
                yield return null;
            }
            GameInput.Simulated.JumpHeld = false;
            Assert.Greater(maxY - y0, 3f, "full jump should clear 3 tiles");
            Assert.Less(maxY - y0, 4.5f);
            yield return new WaitForSeconds(0.8f);
            Assert.IsTrue(player.IsGrounded, "player should land again");
        }

        [UnityTest]
        public IEnumerator Player_ClimbsLadder()
        {
            yield return Load(SceneLoader.Level1);
            yield return TeleportAndSettle(new Vector2(45.5f, 10f));
            Move(0f, 1f);
            yield return new WaitForSeconds(2.2f);
            Move(0f);
            yield return new WaitForSeconds(0.5f);
            Assert.Greater(player.transform.position.y, 16.5f, "player should reach the ledge at the top of the ladder");
            Assert.IsTrue(player.IsGrounded, "player should stand on the one-way ledge after climbing");
        }

        [UnityTest]
        public IEnumerator Shooting_SpawnsProjectileInFront_AndKillsSlime()
        {
            yield return Load(SceneLoader.Level1);
            var slime = GameObject.Find("Enemy_Slime");
            Assert.IsNotNull(slime, "slime should exist in level 1");
            yield return TeleportAndSettle(new Vector2(14.5f, 8f));
            player.SetFacing(1);
            GameInput.Simulated.ShootPressed = true;
            yield return null;
            yield return null;
            var proj = Object.FindFirstObjectByType<Projectile>();
            Assert.IsNotNull(proj, "shooting should spawn a projectile");
            Assert.Greater(proj.transform.position.x, player.transform.position.x + 0.3f, "projectile spawns in front of the player");
            yield return new WaitForSeconds(0.5f);
            GameInput.Simulated.ShootPressed = true;
            yield return new WaitForSeconds(0.6f);
            Assert.IsTrue(slime == null, "two hits should kill the slime");
            Assert.IsFalse(player.GetComponent<PlayerHealth>().IsDead);
        }

        [UnityTest]
        public IEnumerator Shooting_FromBehind_KillsSkeleton()
        {
            yield return Load(SceneLoader.Level1);
            GameObject skeleton = null;
            foreach (var p in Object.FindObjectsByType<PatrolEnemy>(FindObjectsSortMode.None))
                if (Mathf.Abs(p.transform.position.x - 40.5f) < 3f) skeleton = p.gameObject;
            Assert.IsNotNull(skeleton, "the meadow skeleton near x=40");
            var pos = skeleton.transform.position;
            // stand on the opposite side the skeleton is walking towards and shoot it three times
            yield return TeleportAndSettle(new Vector2(pos.x - 4f, 10f));
            for (int i = 0; i < 4 && skeleton != null; i++)
            {
                player.SetFacing(skeleton.transform.position.x > player.transform.position.x ? 1 : -1);
                GameInput.Simulated.ShootPressed = true;
                yield return new WaitForSeconds(0.45f);
            }
            Assert.IsTrue(skeleton == null, "skeleton should die regardless of facing direction");
        }

        [UnityTest]
        public IEnumerator Gem_IsCollected()
        {
            yield return Load(SceneLoader.Level1);
            int before = GameSession.Instance.Gems;
            yield return TeleportAndSettle(new Vector2(8.5f, 8f));
            Assert.Greater(GameSession.Instance.Gems, before);
        }

        [UnityTest]
        public IEnumerator Spikes_DamagePlayer_WithInvulnerability()
        {
            yield return Load(SceneLoader.Level1);
            var health = player.GetComponent<PlayerHealth>();
            yield return TeleportAndSettle(new Vector2(24.5f, 7.2f));
            Assert.AreEqual(2, health.Hearts, "spikes should cost exactly one heart (i-frames prevent repeats)");
            Assert.IsFalse(health.IsDead);
        }

        [UnityTest]
        public IEnumerator Death_ShowsPanel_AndRetryRevives()
        {
            yield return Load(SceneLoader.Level1);
            var health = player.GetComponent<PlayerHealth>();
            health.TakeDamage(new DamageInfo(3, player.transform.position + Vector3.left));
            Assert.IsTrue(health.IsDead);
            yield return new WaitForSeconds(2.2f);
            var panel = GameObject.Find("DeathPanel");
            Assert.IsNotNull(panel, "death panel should be visible");
            LevelManager.Instance.Retry();
            yield return new WaitForSeconds(0.3f);
            Assert.IsFalse(health.IsDead);
            Assert.AreEqual(3, health.Hearts);
            Assert.IsNull(GameObject.Find("DeathPanel"));
        }

        [UnityTest]
        public IEnumerator Pit_CostsHeart_AndRespawns()
        {
            yield return Load(SceneLoader.Level1);
            var health = player.GetComponent<PlayerHealth>();
            yield return TeleportAndSettle(new Vector2(64.5f, 9f));
            yield return new WaitForSeconds(1.5f);
            Assert.AreEqual(2, health.Hearts);
            Assert.Greater(player.transform.position.y, 5f, "player should be back on solid ground");
        }

        [UnityTest]
        public IEnumerator Gate_BlocksWithoutKey_OpensWithKey()
        {
            yield return Load(SceneLoader.Level1);
            yield return TeleportAndSettle(new Vector2(112.8f, 2f));
            Move(1f);
            yield return new WaitForSeconds(1f);
            Assert.Less(player.transform.position.x, 114f, "locked gate should block the player");
            GameSession.Instance.AddKey(KeyType.Gold);
            yield return new WaitForSeconds(1.5f);
            Move(0f);
            Assert.Greater(player.transform.position.x, 116f, "gate should open with the gold key");
            Assert.IsFalse(GameSession.Instance.HasKey(KeyType.Gold), "key is consumed");
        }

        [UnityTest]
        public IEnumerator Projectiles_DoNotTriggerExit()
        {
            yield return Load(SceneLoader.Level1);
            GameSession.Instance.AddKey(KeyType.Gold);
            yield return TeleportAndSettle(new Vector2(117f, 2f));
            player.SetFacing(1);
            for (int i = 0; i < 3; i++)
            {
                GameInput.Simulated.ShootPressed = true;
                yield return new WaitForSeconds(0.35f);
            }
            yield return new WaitForSeconds(1.5f);
            Assert.AreEqual(SceneLoader.Level1, SceneManager.GetActiveScene().name, "shooting the portal must not change level");
        }

        [UnityTest]
        public IEnumerator Portal_LoadsCastle()
        {
            yield return Load(SceneLoader.Level1);
            yield return TeleportAndSettle(new Vector2(121.5f, 2.2f));
            yield return new WaitForSeconds(3f);
            Assert.AreEqual(SceneLoader.Level2, SceneManager.GetActiveScene().name);
            var newPlayer = Object.FindFirstObjectByType<PlayerController>();
            Assert.IsTrue(newPlayer.ControlEnabled);
        }

        [UnityTest]
        public IEnumerator Castle_TreasureDoorNeedsTreasureKey()
        {
            yield return Load(SceneLoader.Level2);
            yield return TeleportAndSettle(new Vector2(124.5f, 3f));
            Move(1f);
            yield return new WaitForSeconds(0.8f);
            Assert.Less(player.transform.position.x, 126f);
            GameSession.Instance.AddKey(KeyType.Gold);
            yield return new WaitForSeconds(0.8f);
            Assert.Less(player.transform.position.x, 126f, "gold key must not open the treasure door");
            GameSession.Instance.AddKey(KeyType.Treasure);
            yield return new WaitForSeconds(0.8f);
            Move(0f);
            Assert.Greater(player.transform.position.x, 128f);
            Assert.Less(player.transform.position.x, 131.5f, "test should stop before the portal");
        }

        [UnityTest]
        public IEnumerator Boss_FightStarts_DiesAndRevealsTreasure_Victory()
        {
            yield return Load(SceneLoader.Boss);
            var boss = Object.FindFirstObjectByType<DragonBoss>();
            Assert.IsNotNull(boss);
            yield return TeleportAndSettle(new Vector2(12.5f, 3f));
            Assert.IsTrue(boss.IsFighting, "entering the arena starts the fight");
            yield return new WaitForSeconds(2.5f);
            var bar = GameObject.Find("BossBar");
            Assert.IsNotNull(bar, "boss health bar is shown");
            // Keep the player safe while the test damages the boss directly.
            player.GetComponent<PlayerHealth>().Heal(3);
            int guard = 0;
            while (!boss.IsDead && guard++ < 100) { boss.TakeDamage(new DamageInfo(1, Vector2.zero)); yield return null; }
            Assert.IsTrue(boss.IsDead);
            yield return new WaitForSeconds(3.5f);
            var chest = GameObject.Find("Chest_Gold");
            Assert.IsNotNull(chest, "treasure should appear after the boss dies");
            yield return TeleportAndSettle(chest.transform.position + Vector3.up * 0.1f);
            yield return new WaitForSeconds(3.5f);
            Assert.IsNotNull(GameObject.Find("VictoryPanel"), "victory screen after claiming the treasure");
        }

        [UnityTest]
        public IEnumerator Boss_ResetsWhenPlayerRetries()
        {
            yield return Load(SceneLoader.Boss);
            var boss = Object.FindFirstObjectByType<DragonBoss>();
            yield return TeleportAndSettle(new Vector2(12.5f, 3f));
            yield return new WaitForSeconds(2.5f);
            boss.TakeDamage(new DamageInfo(5, Vector2.zero));
            var health = player.GetComponent<PlayerHealth>();
            health.TakeDamage(new DamageInfo(3, Vector2.zero));
            yield return new WaitForSeconds(2.2f);
            LevelManager.Instance.Retry();
            yield return new WaitForSeconds(0.3f);
            Assert.IsFalse(boss.IsFighting);
            Assert.AreEqual(boss.maxHealth, boss.Health);
            Assert.Less(player.transform.position.x, 9f, "player respawns before the arena door");
        }

        [UnityTest]
        public IEnumerator MainMenu_PlayStartsLevel1()
        {
            GameInput.Simulated = default;
            SceneManager.LoadScene(SceneLoader.MainMenu);
            yield return new WaitForSeconds(0.5f);
            var play = GameObject.Find("Button_Play");
            Assert.IsNotNull(play);
            play.GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSeconds(2.5f);
            Assert.AreEqual(SceneLoader.Level1, SceneManager.GetActiveScene().name);
        }

        [UnityTest]
        public IEnumerator AllLevels_RunWithoutErrors()
        {
            foreach (var s in new[] { SceneLoader.MainMenu, SceneLoader.Level1, SceneLoader.Level2, SceneLoader.Boss })
            {
                SceneManager.LoadScene(s);
                yield return new WaitForSeconds(3f);
                // LogAssert fails the test automatically on any unexpected error/exception log
            }
        }
    }
}
