using System.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] float moveSpeed = 8f;
    [SerializeField] float jumpSpeed = 25f;
    
    Vector2 moveInput;
    Rigidbody2D rb;
    Animator animator;
    CapsuleCollider2D capsuleCollider;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        capsuleCollider = GetComponent<CapsuleCollider2D>();
    }

    void Update()
    {
        Run();
        FlipSprite();
    }

    void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    void OnJump(InputValue value)
    {
        if (!capsuleCollider.IsTouchingLayers(LayerMask.GetMask("Ground"))) return;
        if (value.isPressed)
            rb.linearVelocity += new Vector2(0f, jumpSpeed);
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
}
