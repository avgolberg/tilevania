using UnityEngine;

namespace TileVania
{
    /// <summary>
    /// Platformer movement: acceleration-based running, buffered/coyote jumps with variable height,
    /// ladder climbing, one-way platform drop-through, and projectile shooting.
    /// Input is read in Update, physics is applied in FixedUpdate.
    /// The transform pivot is at the feet; the sprite lives on the child "Visual".
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Run")]
        public float moveSpeed = 7.5f;
        public float groundAcceleration = 90f;
        public float groundDeceleration = 100f;
        public float airAcceleration = 55f;
        public float airDeceleration = 40f;

        [Header("Jump")]
        public float jumpVelocity = 16.5f;
        public float gravityScale = 4f;
        public float fallGravityMultiplier = 1.6f;
        public float jumpCutMultiplier = 0.45f;
        public float maxFallSpeed = 22f;
        public float coyoteTime = 0.1f;
        public float jumpBufferTime = 0.12f;

        [Header("Climb")]
        public float climbSpeed = 4.5f;

        [Header("Shoot")]
        public Projectile projectilePrefab;
        public float shootCooldown = 0.3f;
        public Vector2 muzzleOffset = new Vector2(0.6f, 0.62f);

        [Header("References")]
        public Transform visual;
        public Animator animator;

        static readonly int IdleState = Animator.StringToHash("Idle");
        static readonly int RunState = Animator.StringToHash("Run");
        static readonly int JumpState = Animator.StringToHash("Jump");
        static readonly int ApexState = Animator.StringToHash("Apex");
        static readonly int FallState = Animator.StringToHash("Fall");
        static readonly int ClimbState = Animator.StringToHash("Climb");
        static readonly int AttackState = Animator.StringToHash("Attack");
        static readonly int HurtState = Animator.StringToHash("Hurt");

        Rigidbody2D rb;
        Vector2 input;
        float lastGroundedTime = -10f, lastJumpPressedTime = -10f;
        float shootTimer, attackAnimTimer, knockbackTimer, dropThroughTimer, hurtAnimTimer;
        bool jumpHeld, wasGrounded;
        int currentAnim;
        float prevVelocityY;
        Vector2 squash = Vector2.one;

        public bool IsGrounded { get; private set; }
        public bool IsClimbing { get; private set; }
        public int Facing { get; private set; } = 1;
        public bool ControlEnabled { get; private set; } = true;
        public Rigidbody2D Body => rb;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = gravityScale;
        }

        void OnDisable() => SetOneWayIgnored(false);

        public void SetControl(bool enabled)
        {
            ControlEnabled = enabled;
            if (!enabled)
            {
                input = Vector2.zero;
                if (IsClimbing) StopClimbing();
            }
        }

        public void Teleport(Vector2 position)
        {
            StopClimbing();
            rb.position = position;
            transform.position = position;
            rb.linearVelocity = Vector2.zero;
            knockbackTimer = 0f;
        }

        public void Knockback(Vector2 velocity, float duration)
        {
            if (IsClimbing) StopClimbing();
            rb.linearVelocity = velocity;
            knockbackTimer = duration;
            hurtAnimTimer = 0.35f;
        }

        public void SetFacing(int dir)
        {
            if (dir != 0) Facing = dir > 0 ? 1 : -1;
        }

        void Update()
        {
            if (Time.timeScale == 0f) return;
            float dt = Time.deltaTime;
            shootTimer -= dt; attackAnimTimer -= dt; dropThroughTimer -= dt; hurtAnimTimer -= dt;

            if (ControlEnabled)
            {
                input = GameInput.Move;
                jumpHeld = GameInput.JumpHeld;
                if (GameInput.JumpPressed) lastJumpPressedTime = Time.time;
                if (GameInput.ShootPressed) TryShoot();
                if (knockbackTimer <= 0f && Mathf.Abs(input.x) > 0.2f && !IsClimbing) SetFacing((int)Mathf.Sign(input.x));
                UpdateLadder();
            }
            else
            {
                input = Vector2.zero;
                jumpHeld = false;
            }

            UpdateVisual(dt);
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            knockbackTimer -= dt;
            CheckGround();

            if (IsClimbing)
            {
                rb.gravityScale = 0f;
                rb.linearVelocity = new Vector2(0f, input.y * climbSpeed);
                if (Time.time - lastJumpPressedTime <= jumpBufferTime)
                {
                    lastJumpPressedTime = -10f;
                    StopClimbing();
                    rb.linearVelocity = new Vector2(input.x * moveSpeed, jumpVelocity * 0.75f);
                    OnJumped();
                }
                return;
            }

            var v = rb.linearVelocity;

            // Horizontal movement
            if (knockbackTimer <= 0f)
            {
                float target = input.x * moveSpeed;
                bool accelerating = Mathf.Abs(target) > 0.01f;
                float rate = IsGrounded
                    ? (accelerating ? groundAcceleration : groundDeceleration)
                    : (accelerating ? airAcceleration : airDeceleration);
                v.x = Mathf.MoveTowards(v.x, target, rate * dt);
            }

            // Jump (buffered + coyote time); Down + Jump on a one-way platform drops through it
            bool buffered = Time.time - lastJumpPressedTime <= jumpBufferTime;
            bool canJump = Time.time - lastGroundedTime <= coyoteTime;
            if (buffered && canJump && ControlEnabled)
            {
                lastJumpPressedTime = -10f;
                if (input.y < -0.5f && StandingOnOneWay())
                {
                    dropThroughTimer = 0.3f;
                    SetOneWayIgnored(true);
                    lastGroundedTime = -10f;
                }
                else
                {
                    v.y = jumpVelocity;
                    lastGroundedTime = -10f;
                    OnJumped();
                }
            }

            // Variable jump height and snappier falling
            float g = gravityScale;
            if (v.y < 0f) g *= fallGravityMultiplier;
            else if (v.y > 0f && !jumpHeld && knockbackTimer <= 0f) v.y *= Mathf.Pow(jumpCutMultiplier, dt * 10f);
            rb.gravityScale = g;
            v.y = Mathf.Max(v.y, -maxFallSpeed);

            if (dropThroughTimer <= 0f && !IsClimbing) SetOneWayIgnored(false);

            prevVelocityY = v.y;
            rb.linearVelocity = v;
        }

        void CheckGround()
        {
            int mask = dropThroughTimer > 0f ? Layers.SolidMask : Layers.GroundMask;
            bool hit = false;
            // Rising bodies are never grounded, so jumping up through one-way platforms doesn't re-ground mid-jump.
            if (rb.linearVelocity.y <= 0.1f)
            {
                var box = Physics2D.OverlapBox((Vector2)transform.position + new Vector2(0f, -0.03f), new Vector2(0.55f, 0.08f), 0f, mask);
                hit = box != null;
            }
            IsGrounded = hit;
            if (IsGrounded)
            {
                lastGroundedTime = Time.time;
                if (!wasGrounded && prevVelocityY < -4f) OnLanded(prevVelocityY);
            }
            wasGrounded = IsGrounded;
        }

        bool StandingOnOneWay()
        {
            var solid = Physics2D.OverlapBox((Vector2)transform.position + new Vector2(0f, -0.03f), new Vector2(0.55f, 0.08f), 0f, Layers.SolidMask);
            if (solid != null) return false;
            return Physics2D.OverlapBox((Vector2)transform.position + new Vector2(0f, -0.03f), new Vector2(0.55f, 0.08f), 0f, 1 << Layers.OneWay) != null;
        }

        void UpdateLadder()
        {
            Vector2 pos = transform.position;
            bool bodyOnLadder = Physics2D.OverlapBox(pos + new Vector2(0f, 0.7f), new Vector2(0.3f, 1.2f), 0f, Layers.LadderMask) != null;
            bool ladderBelow = Physics2D.OverlapPoint(pos + new Vector2(0f, -0.3f), Layers.LadderMask) != null;

            if (!IsClimbing)
            {
                if (knockbackTimer > 0f) return;
                bool up = input.y > 0.5f && bodyOnLadder;
                bool down = input.y < -0.5f && ladderBelow && IsGrounded;
                if (up || down) StartClimbing();
                return;
            }

            if (!bodyOnLadder && !ladderBelow) { StopClimbing(); return; }
            if (Mathf.Abs(input.x) > 0.6f && Mathf.Abs(input.y) < 0.3f) { StopClimbing(); return; }
            if (input.y < -0.5f && Physics2D.OverlapBox(pos + new Vector2(0f, -0.03f), new Vector2(0.55f, 0.08f), 0f, Layers.SolidMask) != null)
                StopClimbing();
        }

        void StartClimbing()
        {
            IsClimbing = true;
            SetOneWayIgnored(true);
            var p = rb.position;
            p.x = Mathf.Floor(p.x) + 0.5f; // ladders are one tile wide on an integer grid
            rb.position = p;
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f;
        }

        void StopClimbing()
        {
            if (!IsClimbing) return;
            IsClimbing = false;
            rb.gravityScale = gravityScale;
            dropThroughTimer = 0.05f;
        }

        void SetOneWayIgnored(bool ignored)
        {
            if (Physics2D.GetIgnoreLayerCollision(Layers.Player, Layers.OneWay) != ignored)
                Physics2D.IgnoreLayerCollision(Layers.Player, Layers.OneWay, ignored);
        }

        void TryShoot()
        {
            if (shootTimer > 0f || IsClimbing || projectilePrefab == null) return;
            shootTimer = shootCooldown;
            attackAnimTimer = 0.25f;
            // Hold Up to shoot straight up (useful against flying enemies and the boss).
            bool up = input.y > 0.5f;
            Vector2 dir = up ? Vector2.up : new Vector2(Facing, 0f);
            Vector2 origin = (Vector2)transform.position + new Vector2(0f, up ? 1.2f : muzzleOffset.y);
            Vector2 spawn = up ? origin + Vector2.up * 0.5f : origin + new Vector2(muzzleOffset.x * Facing, 0f);
            AudioManager.Play(Sfx.Shoot);
            // Never spawn inside a wall (or behind it): just show a spark when point-blank against one.
            if (Physics2D.Linecast(origin, spawn, Layers.SolidMask))
            {
                Fx.Burst(origin + dir * 0.3f, new Color(0.8f, 0.9f, 1f), 5, 2.5f, 0.2f, 0f, 1.5f);
                return;
            }
            var p = Instantiate(projectilePrefab, spawn, Quaternion.identity);
            p.Launch(dir);
        }

        void OnJumped()
        {
            AudioManager.Play(Sfx.Jump, 0.6f);
            Fx.Dust(transform.position, 5);
            squash = new Vector2(0.8f, 1.2f);
        }

        void OnLanded(float impactVelocity)
        {
            AudioManager.Play(Sfx.Land, Mathf.Clamp01(-impactVelocity / 20f));
            Fx.Dust(transform.position, 6);
            squash = new Vector2(1.2f, 0.8f);
        }

        void UpdateVisual(float dt)
        {
            if (visual == null) return;
            squash = Vector2.MoveTowards(squash, Vector2.one, dt * 4f);
            visual.localScale = new Vector3(Facing * squash.x, squash.y, 1f);
            if (animator == null) return;

            int state;
            var v = rb.linearVelocity;
            if (hurtAnimTimer > 0f) state = HurtState;
            else if (IsClimbing) state = ClimbState;
            else if (attackAnimTimer > 0f) state = AttackState;
            else if (!IsGrounded)
                state = v.y > 3f ? JumpState : (v.y > -3f ? ApexState : FallState);
            else state = Mathf.Abs(v.x) > 0.5f ? RunState : IdleState;

            animator.speed = state == ClimbState ? (Mathf.Abs(v.y) > 0.1f ? 1f : 0f) : 1f;
            if (state != currentAnim)
            {
                currentAnim = state;
                animator.Play(state, 0, 0f);
            }
        }
    }
}
