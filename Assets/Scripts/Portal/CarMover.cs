// CarMover.cs 1.0.0
//
// Top-down driving: W or Up goes, S or Down brakes and reverses, A and D or Left
// and Right steer. Hold Space to drift.
//
// 1. Drop this file anywhere in Assets/ (not in Assets/Editor/).
// 2. Use a car sprite that points up. If yours points another way, rotate it in
//    an image editor, or put the sprite on a child object and turn the child.
// 3. Select the car. Add Component > Rigidbody 2D, a Collider 2D, then CarMover.
// 4. Press Play.
//
// Drifting: the car turns sharper and slides, because it stops following where
// it's pointing as closely. Let off the gas while you hold Space and it slows
// down. Change the numbers in the Inspector while the game is playing to tune
// the feel, and write down the ones you like: changes made during Play are lost.

using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class CarMover : MonoBehaviour
{
    [Tooltip("How fast the car can go, in units per second.")]
    public float topSpeed = 10f;

    [Tooltip("How quickly the car speeds up.")]
    public float acceleration = 8f;

    [Tooltip("How quickly the car slows down on its own when you let off the gas.")]
    public float coasting = 2f;

    [Tooltip("How fast the car turns at top speed, in degrees per second.")]
    public float turnSpeed = 180f;

    [Tooltip("How closely the car follows where it's pointing. Bigger means less slide.")]
    public float grip = 8f;

    [Tooltip("The same thing while drifting. Smaller means more slide.")]
    public float driftGrip = 1.5f;

    [Tooltip("How much sharper the car turns while drifting. 1.5 is half again as sharp.")]
    public float driftTurn = 1.5f;

    [Tooltip("How quickly the car slows down when you hold Space and let off the gas.")]
    public float driftBrake = 4f;

    Rigidbody2D body;
    float gas;
    float steer;
    bool drifting;
    float speed = 0f;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
    }

    void Update()
    {
        Keyboard keys = Keyboard.current;
        if (keys == null) return;

        gas = 0f;
        if (keys.wKey.isPressed || keys.upArrowKey.isPressed) gas = gas + 1f;
        if (keys.sKey.isPressed || keys.downArrowKey.isPressed) gas = gas - 1f;

        steer = 0f;
        if (keys.aKey.isPressed || keys.leftArrowKey.isPressed) steer = steer + 1f;
        if (keys.dKey.isPressed || keys.rightArrowKey.isPressed) steer = steer - 1f;

        drifting = keys.spaceKey.isPressed;
    }

    void FixedUpdate()
    {
        float step = Time.fixedDeltaTime;

        if (gas != 0f)
        {
            speed = Mathf.MoveTowards(speed, gas * topSpeed, acceleration * step);
        }
        else
        {
            float slowing = drifting ? driftBrake : coasting;
            speed = Mathf.MoveTowards(speed, 0f, slowing * step);
        }

        // A parked car can't turn, and turning scales with speed.
        float turn = steer * turnSpeed * (speed / topSpeed) * step;
        if (drifting) turn = turn * driftTurn;
        body.rotation = body.rotation + turn;

        Vector2 pointing = Quaternion.Euler(0f, 0f, body.rotation) * Vector2.up;
        float followStrength = drifting ? driftGrip : grip;
        body.linearVelocity = Vector2.Lerp(body.linearVelocity, pointing * speed, followStrength * step);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        speed = speed / 2f;
    }
}
