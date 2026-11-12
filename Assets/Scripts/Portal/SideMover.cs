// SideMover.cs 1.0.0
//
// Platformer movement: run left and right, jump, fall.
//
// 1. Drop this file anywhere in Assets/ (not in Assets/Editor/).
// 2. Select your player. Add Component > Rigidbody 2D, then a Collider 2D
//    (Box or Capsule), then SideMover.
// 3. Your ground needs a collider too (a Tilemap Collider 2D works).
// 4. Press Play. A/D or the arrow keys move. Space, W or Up jumps.
//
// Change the numbers in the Inspector while the game is playing to tune the
// feel. Changes made during Play are lost when you stop, so write down the
// numbers you like.

using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class SideMover : MonoBehaviour
{
    [Tooltip("How fast the player runs, in units per second.")]
    public float moveSpeed = 6f;

    [Tooltip("How fast the player leaves the ground when jumping. Bigger means a higher jump.")]
    public float jumpSpeed = 12f;

    [Tooltip("How strongly the player is pulled down. 1 is normal Unity gravity; 3 feels snappier.")]
    public float gravity = 3f;

    [Tooltip("Flip the sprite to face the way the player is running.")]
    public bool faceMoveDirection = true;

    Rigidbody2D body;
    SpriteRenderer sprite;
    ContactFilter2D groundFilter;
    float moveInput;
    bool jumpPressed;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        body.freezeRotation = true;

        // Ground is anything we touch from above: the contact pushes up on us.
        groundFilter.useNormalAngle = true;
        groundFilter.minNormalAngle = 45f;
        groundFilter.maxNormalAngle = 135f;
    }

    void Update()
    {
        var keys = Keyboard.current;
        if (keys == null) return;

        moveInput = 0f;
        if (keys.aKey.isPressed || keys.leftArrowKey.isPressed) moveInput -= 1f;
        if (keys.dKey.isPressed || keys.rightArrowKey.isPressed) moveInput += 1f;

        if (keys.spaceKey.wasPressedThisFrame || keys.wKey.wasPressedThisFrame || keys.upArrowKey.wasPressedThisFrame)
            jumpPressed = true;

        if (faceMoveDirection && sprite != null && moveInput != 0f)
            sprite.flipX = moveInput < 0f;
    }

    void FixedUpdate()
    {
        body.gravityScale = gravity;

        Vector2 velocity = body.linearVelocity;
        velocity.x = moveInput * moveSpeed;
        if (jumpPressed && IsGrounded())
            velocity.y = jumpSpeed;
        jumpPressed = false;

        body.linearVelocity = velocity;
    }

    public bool IsGrounded()
    {
        return body.IsTouching(groundFilter);
    }
}
