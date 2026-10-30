using UnityEngine;
using UnityEngine.InputSystem;

namespace TileVania
{
    /// <summary>
    /// Gameplay input built in code on top of the Input System, so no scene wiring is needed.
    /// Keyboard is the primary target; gamepad bindings are included as a bonus.
    /// Tests can drive the player through <see cref="Simulated"/>.
    /// </summary>
    public static class GameInput
    {
        public struct SimulatedInput
        {
            public bool Enabled;
            public Vector2 Move;
            public bool JumpHeld;
            public bool JumpPressed;
            public bool ShootPressed;
        }

        public static SimulatedInput Simulated;

        static InputAction move, jump, shoot, pause;
        static bool initialized;

        static void Init()
        {
            if (initialized) return;
            initialized = true;

            move = new InputAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            move.AddBinding("<Gamepad>/leftStick");
            move.AddBinding("<Gamepad>/dpad");

            jump = new InputAction("Jump", InputActionType.Button);
            jump.AddBinding("<Keyboard>/space");
            jump.AddBinding("<Keyboard>/k");
            jump.AddBinding("<Gamepad>/buttonSouth");

            shoot = new InputAction("Shoot", InputActionType.Button);
            shoot.AddBinding("<Keyboard>/j");
            shoot.AddBinding("<Keyboard>/x");
            shoot.AddBinding("<Keyboard>/leftCtrl");
            shoot.AddBinding("<Mouse>/leftButton");
            shoot.AddBinding("<Gamepad>/buttonWest");

            pause = new InputAction("Pause", InputActionType.Button);
            pause.AddBinding("<Keyboard>/escape");
            pause.AddBinding("<Keyboard>/p");
            pause.AddBinding("<Gamepad>/start");

            move.Enable(); jump.Enable(); shoot.Enable(); pause.Enable();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            initialized = false;
            Simulated = default;
        }

        public static Vector2 Move
        {
            get
            {
                if (Simulated.Enabled) return Simulated.Move;
                Init();
                return move.ReadValue<Vector2>();
            }
        }

        public static bool JumpPressed
        {
            get
            {
                if (Simulated.Enabled) { bool v = Simulated.JumpPressed; Simulated.JumpPressed = false; return v; }
                Init();
                return jump.WasPressedThisFrame();
            }
        }

        public static bool JumpHeld
        {
            get
            {
                if (Simulated.Enabled) return Simulated.JumpHeld;
                Init();
                return jump.IsPressed();
            }
        }

        public static bool ShootPressed
        {
            get
            {
                if (Simulated.Enabled) { bool v = Simulated.ShootPressed; Simulated.ShootPressed = false; return v; }
                Init();
                return shoot.WasPressedThisFrame();
            }
        }

        public static bool PausePressed
        {
            get
            {
                if (Simulated.Enabled) return false;
                Init();
                return pause.WasPressedThisFrame();
            }
        }
    }
}
