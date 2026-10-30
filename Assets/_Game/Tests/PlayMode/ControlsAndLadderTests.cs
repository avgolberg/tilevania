using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TileVania.Tests
{
    /// <summary>
    /// Iteration 2 coverage: context-sensitive keyboard controls, ladders in both directions,
    /// one-way drop-through, object placement/sorting in generated scenes and HUD toast lifecycle.
    /// Coordinates match the ASCII layouts in Assets/_Game/Levels.
    /// </summary>
    public class ControlsAndLadderTests
    {
        PlayerController player;

        IEnumerator Load(string scene)
        {
            while (SceneLoader.IsLoading) yield return null;
            GameInput.Simulated = new GameInput.SimulatedInput { Enabled = true };
            SceneManager.LoadScene(scene);
            yield return null;
            yield return null;
            player = Object.FindFirstObjectByType<PlayerController>();
            yield return new WaitForSeconds(0.4f);
        }

        [TearDown]
        public void TearDown()
        {
            GameInput.Simulated = default;
            Time.timeScale = 1f;
        }

        void Move(float x, float y) => GameInput.Simulated.Move = new Vector2(x, y);

        IEnumerator Teleport(Vector2 pos)
        {
            player.Teleport(pos);
            yield return new WaitForSeconds(0.4f);
        }

        float VelY => player.Body.linearVelocity.y;

        // ------------------------------------------------------------------ controls

        [UnityTest]
        public IEnumerator UpArrow_Jumps_AwayFromLadders()
        {
            yield return Load(SceneLoader.Level1);
            float y0 = player.transform.position.y;
            Move(0f, 1f);
            GameInput.Simulated.UpPressed = true;
            yield return new WaitForSeconds(0.3f);
            Assert.Greater(player.transform.position.y, y0 + 1.5f, "Up arrow should jump when no ladder is near");
            Move(0f, 0f);
        }

        [UnityTest]
        public IEnumerator UpArrow_OnLadder_ClimbsInsteadOfJumping()
        {
            yield return Load(SceneLoader.Level1);
            yield return Teleport(new Vector2(45.5f, 10f));
            Move(0f, 1f);
            GameInput.Simulated.UpPressed = true;
            yield return new WaitForSeconds(0.15f);
            Assert.IsTrue(player.IsClimbing, "Up arrow next to a ladder should grab it");
            Assert.LessOrEqual(VelY, player.climbSpeed + 0.01f, "climbing speed, not a jump");
            Move(0f, 0f);
        }

        [UnityTest]
        public IEnumerator UpPlusX_ShootsUp_WithoutJumping([Values(0, 2)] int framesBetween)
        {
            yield return Load(SceneLoader.Level1);
            float y0 = player.transform.position.y;
            Move(0f, 1f);
            GameInput.Simulated.UpPressed = true;
            for (int i = 0; i < framesBetween; i++) yield return null;
            GameInput.Simulated.ShootPressed = true;
            yield return null;
            yield return null;
            var proj = Object.FindFirstObjectByType<Projectile>();
            Assert.IsNotNull(proj, "X should fire");
            Assert.Greater(proj.GetComponent<Rigidbody2D>().linearVelocity.y, 10f, "Up + X fires upward");
            yield return new WaitForSeconds(0.4f);
            Assert.Less(Mathf.Abs(player.transform.position.y - y0), 0.05f, "Up + X must not trigger a jump");
            Assert.IsTrue(player.IsGrounded);
            Move(0f, 0f);
        }

        [UnityTest]
        public IEnumerator X_ShootsForward()
        {
            yield return Load(SceneLoader.Level1);
            player.SetFacing(1);
            GameInput.Simulated.ShootPressed = true;
            yield return null;
            yield return null;
            var proj = Object.FindFirstObjectByType<Projectile>();
            Assert.IsNotNull(proj);
            Assert.Greater(proj.GetComponent<Rigidbody2D>().linearVelocity.x, 10f);
        }

        [UnityTest]
        public IEnumerator ZJump_HasVariableHeight()
        {
            yield return Load(SceneLoader.Level1);
            float y0 = player.transform.position.y, tapMax = y0;
            GameInput.Simulated.JumpPressed = true;
            GameInput.Simulated.JumpHeld = true;
            yield return new WaitForSeconds(0.05f);
            GameInput.Simulated.JumpHeld = false;
            for (float t = 0; t < 0.8f; t += Time.deltaTime) { tapMax = Mathf.Max(tapMax, player.transform.position.y); yield return null; }
            Assert.Greater(tapMax - y0, 0.5f);
            Assert.Less(tapMax - y0, 2.5f, "a tapped jump should be clearly lower than a held one");
        }

        [UnityTest]
        public IEnumerator DownPlusZ_DropsThroughLedge_DownAloneDoesNot()
        {
            yield return Load(SceneLoader.Level1);
            yield return Teleport(new Vector2(13.5f, 11.2f)); // one-way ledge, cells x12..15 at y10
            Assert.IsTrue(player.IsGrounded);
            Move(0f, -1f);
            yield return new WaitForSeconds(0.5f);
            Assert.Greater(player.transform.position.y, 10.9f, "holding Down alone must not drop through a ledge");
            GameInput.Simulated.JumpPressed = true;
            yield return new WaitForSeconds(0.7f);
            Move(0f, 0f);
            Assert.Less(player.transform.position.y, 9f, "Down + Z drops through the ledge");
            Assert.IsTrue(player.IsGrounded);
        }

        [UnityTest]
        public IEnumerator Ladder_JumpOffWithZ()
        {
            yield return Load(SceneLoader.Level1);
            yield return Teleport(new Vector2(45.5f, 10f));
            Move(0f, 1f);
            yield return new WaitForSeconds(0.6f);
            Move(0f, 0f);
            yield return null;
            GameInput.Simulated.JumpPressed = true;
            GameInput.Simulated.JumpHeld = true;
            yield return new WaitForSeconds(0.1f);
            GameInput.Simulated.JumpHeld = false;
            Assert.IsFalse(player.IsClimbing, "Z jumps off the ladder");
            Assert.Greater(VelY, 4f, "jumping off gives upward speed");
            Move(0f, 0f);
        }

        // ------------------------------------------------------------------ ladders

        [UnityTest]
        public IEnumerator Ladder_DescendFromSurfaceIntoCave([Values(78.5f, 78.2f, 78.8f, 79.25f)] float startX)
        {
            yield return Load(SceneLoader.Level1);
            yield return Teleport(new Vector2(startX, 10f));
            Move(0f, -1f);
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(player.IsClimbing, $"Down at x={startX} on top of the shaft should grab the ladder");
            yield return new WaitForSeconds(2.2f);
            Move(0f, 0f);
            yield return new WaitForSeconds(0.2f);
            Assert.Less(player.transform.position.y, 2.2f, "player should reach the cave floor");
            Assert.IsFalse(player.IsClimbing, "player steps off at the bottom");
            Assert.IsTrue(player.IsGrounded);
        }

        [UnityTest]
        public IEnumerator Ladder_StopMidway_ThenContinueBothWays()
        {
            yield return Load(SceneLoader.Level1);
            yield return Teleport(new Vector2(78.5f, 10f));
            Move(0f, -1f);
            yield return new WaitForSeconds(0.8f);
            Move(0f, 0f);
            yield return new WaitForSeconds(0.1f);
            float mid = player.transform.position.y;
            Assert.IsTrue(player.IsClimbing);
            Assert.Less(mid, 9.5f);
            yield return new WaitForSeconds(0.6f);
            Assert.Less(Mathf.Abs(player.transform.position.y - mid), 0.02f, "player holds position when input is released");
            Move(0f, 1f);
            yield return new WaitForSeconds(2f);
            Move(0f, 0f);
            yield return new WaitForSeconds(0.3f);
            Assert.Greater(player.transform.position.y, 9.9f, "climbing back up exits at the top");
            Assert.IsFalse(player.IsClimbing);
            Assert.IsTrue(player.IsGrounded);
        }

        [UnityTest]
        public IEnumerator Ladder_ClimbUpShaftFromCave()
        {
            yield return Load(SceneLoader.Level1);
            yield return Teleport(new Vector2(78.5f, 2f));
            Move(0f, 1f);
            yield return new WaitForSeconds(2.6f);
            Move(0f, 0f);
            yield return new WaitForSeconds(0.3f);
            Assert.Greater(player.transform.position.y, 9.9f);
            Assert.IsTrue(player.IsGrounded);
        }

        [UnityTest]
        public IEnumerator Ladder_DescendOntoOneWayLedge_StopsOnIt()
        {
            yield return Load(SceneLoader.Level2);
            yield return Teleport(new Vector2(67.5f, 20f)); // top of the upper tower ladder (cells y12..18, top at y19)
            Move(0f, -1f);
            yield return new WaitForSeconds(2.6f);
            Move(0f, 0f);
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(12f, player.transform.position.y, 0.15f, "player should stop on the wooden floor at the ladder's foot");
            Assert.IsFalse(player.IsClimbing);
            Assert.IsTrue(player.IsGrounded);
        }

        [UnityTest]
        public IEnumerator Ladder_DownNextToLadderBottom_DoesNotSnap()
        {
            yield return Load(SceneLoader.Level1);
            yield return Teleport(new Vector2(46.3f, 10f)); // beside the ladder foot at x=45
            Move(0f, -1f);
            yield return new WaitForSeconds(0.3f);
            Move(0f, 0f);
            Assert.IsFalse(player.IsClimbing);
            Assert.AreEqual(46.3f, player.transform.position.x, 0.05f);
        }

        // ------------------------------------------------------------------ placement & sorting

        [UnityTest]
        public IEnumerator Objects_AreNotEmbedded_AndDrawInFrontOfTerrain(
            [Values(SceneLoader.Level1, SceneLoader.Level2, SceneLoader.Boss)] string scene)
        {
            GameInput.Simulated = new GameInput.SimulatedInput { Enabled = true };
            SceneManager.LoadScene(scene);
            yield return null;
            yield return new WaitForFixedUpdate();

            int platforms = SortingLayer.GetLayerValueFromName("Platforms");
            var problems = new List<string>();

            // Nothing in the terrain may draw over gameplay objects.
            foreach (var tr in Object.FindObjectsByType<UnityEngine.Tilemaps.TilemapRenderer>(FindObjectsSortMode.None))
                if (SortingLayer.GetLayerValueFromName(tr.sortingLayerName) > SortingLayer.GetLayerValueFromName("Ladder"))
                    problems.Add($"tilemap {tr.name} draws above gameplay objects ({tr.sortingLayerName})");

            var level = GameObject.Find("Level").transform;
            foreach (Transform group in level)
            {
                if (group.name == "Decor" || group.name == "Lights" || group.name == "Boss") continue;
                foreach (Transform obj in group)
                {
                    foreach (var sr in obj.GetComponentsInChildren<SpriteRenderer>())
                    {
                        if (sr.sprite == null || sr.name == "Thread") continue;
                        if (group.name != "Gates" && SortingLayer.GetLayerValueFromName(sr.sortingLayerName) <= platforms)
                            problems.Add($"{obj.name} sorted at/below terrain ({sr.sortingLayerName})");
                        // Visible sprite must not overlap solid terrain (1 px tolerance + bob).
                        var b = sr.bounds;
                        var size = new Vector2(Mathf.Max(0.01f, b.size.x - 0.25f), Mathf.Max(0.01f, b.size.y - 0.25f));
                        foreach (var hit in Physics2D.OverlapBoxAll(b.center, size, 0f, Layers.SolidMask))
                            if (!hit.transform.IsChildOf(obj))
                                problems.Add($"{obj.name} at {obj.position} overlaps terrain '{hit.name}'");
                    }

                    // Ground-based objects must stand on something.
                    if (group.name is "Chests" or "Hazards" or "Checkpoints" or "Exit"
                        || obj.GetComponent<PatrolEnemy>() != null || obj.name.StartsWith("Enemy_Slime"))
                    {
                        var hit = Physics2D.Raycast((Vector2)obj.position + Vector2.up * 0.1f, Vector2.down, 0.25f, Layers.GroundMask);
                        if (hit.collider == null) problems.Add($"{obj.name} at {obj.position} is not standing on ground");
                    }
                }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        // ------------------------------------------------------------------ notifications

        [UnityTest]
        public IEnumerator Toast_FadesOut_AndDoesNotSurviveSceneChange()
        {
            yield return Load(SceneLoader.Level1);
            var hud = Object.FindFirstObjectByType<HUD>();
            yield return new WaitForSeconds(3.5f); // let the level title card finish
            HUD.ShowMessage("Found the Gold Key! It opens a locked gate.");
            yield return new WaitForSeconds(0.5f);
            Assert.Greater(hud.messageGroup.alpha, 0.9f, "toast is visible");
            yield return new WaitForSeconds(3f);
            Assert.AreEqual(0f, hud.messageGroup.alpha, 0.001f, "toast faded out after ~3 s");
            Assert.AreEqual(0f, hud.titleGroup.alpha, 0.001f, "title card faded out");

            HUD.ShowMessage("Found the Gold Key! It opens a locked gate.");
            yield return new WaitForSeconds(0.2f);
            SceneManager.LoadScene(SceneLoader.Level2);
            yield return null;
            yield return null;
            var newHud = Object.FindFirstObjectByType<HUD>();
            Assert.AreNotSame(hud, newHud);
            Assert.AreEqual(0f, newHud.messageGroup.alpha, 0.001f, "no toast carried into the next scene");
        }
    }
}
