// TopDownMover.cs 1.0.0
//
// Top-down movement: walk up, down, left and right, with no gravity.
//
// 1. Drop this file anywhere in Assets/ (not in Assets/Editor/).
// 2. Select your player. Add Component > Rigidbody 2D, then a Collider 2D
//    (Box or Circle), then TopDownMover.
// 3. Walls need a collider too (a Tilemap Collider 2D works).
// 4. Press Play. WASD or the arrow keys move.
//
// Change the numbers in the Inspector while the game is playing to tune the
// feel. Changes made during Play are lost when you stop, so write down the
// numbers you like.

using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class TopDownMover : MonoBehaviour
{
    [Tooltip("How fast the player walks, in units per second.")]
    public float moveSpeed = 5f;

    [Tooltip("Flip the sprite to face the way the player is walking left or right.")]
    public bool faceMoveDirection = true;

    Rigidbody2D body;
    SpriteRenderer sprite;
    Vector2 moveInput;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        body.freezeRotation = true;
        body.gravityScale = 0f;
    }

    void Update()
    {
        var keys = Keyboard.current;
        if (keys == null) return;

        moveInput = Vector2.zero;
        if (keys.aKey.isPressed || keys.leftArrowKey.isPressed) moveInput.x -= 1f;
        if (keys.dKey.isPressed || keys.rightArrowKey.isPressed) moveInput.x += 1f;
        if (keys.sKey.isPressed || keys.downArrowKey.isPressed) moveInput.y -= 1f;
        if (keys.wKey.isPressed || keys.upArrowKey.isPressed) moveInput.y += 1f;

        // Without this, moving diagonally is about 1.4 times faster.
        moveInput = Vector2.ClampMagnitude(moveInput, 1f);

        if (faceMoveDirection && sprite != null && moveInput.x != 0f)
            sprite.flipX = moveInput.x < 0f;
    }

    void FixedUpdate()
    {
        body.linearVelocity = moveInput * moveSpeed;
    }
}
