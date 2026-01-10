using System.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] float moveSpeed = 8f;
    [SerializeField] float climbSpeed = 5f;
    [SerializeField] float jumpSpeed = 25f;

    Vector2 moveInput;
    Rigidbody2D rb;
    Animator animator;
    CapsuleCollider2D bodyCollider;
    BoxCollider2D feetCollider;
    float gravityScaleAtStart;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        bodyCollider = GetComponent<CapsuleCollider2D>();
        feetCollider = GetComponent<BoxCollider2D>();
        gravityScaleAtStart = rb.gravityScale;
    }

    void Update()
    {
        Run();
        FlipSprite();
        ClimbLadder();
    }

    void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    void OnJump(InputValue value)
    {
        if (!feetCollider.IsTouchingLayers(LayerMask.GetMask("Ground"))) return;
        if (value.isPressed)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y + jumpSpeed);
    }

    void Run()
    {
        Vector2 playerVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);
        rb.linearVelocity = playerVelocity;
        bool isRunning = Mathf.Abs(rb.linearVelocity.x) > Mathf.Epsilon;
        animator.SetBool("isRunning", isRunning);
    }

    void FlipSprite()
    {
        bool isMoving = Mathf.Abs(rb.linearVelocity.x) > Mathf.Epsilon;
        if (isMoving)
            transform.localScale = new Vector2(Mathf.Sign(rb.linearVelocity.x), 1f);
    }

    void ClimbLadder()
    {
        if (!feetCollider.IsTouchingLayers(LayerMask.GetMask("Ladder")))
        {
            rb.gravityScale = gravityScaleAtStart;
            animator.SetBool("isClimbing", false);
            return;
        }
        
        rb.gravityScale = 0f;
        Vector2 playerVelocity = new Vector2(rb.linearVelocity.x, moveInput.y * climbSpeed);
        rb.linearVelocity = playerVelocity;

        bool isClimbing = Mathf.Abs(rb.linearVelocity.y) > Mathf.Epsilon;
        animator.SetBool("isClimbing", isClimbing);
    }
}
