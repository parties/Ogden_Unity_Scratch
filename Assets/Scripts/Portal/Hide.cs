// Hide.cs 1.0.0
//
// Press Q to hide. While you're hidden, enemies can't see you and you can't move.
// An enemy that was already chasing you keeps chasing for a few seconds
// (Keep Chasing After Hide on the enemy).
//
// 1. Drop this file anywhere in Assets/ (not in Assets/Editor/).
//    EnemyChase.cs has to be in your project too, even in a platformer.
// 2. Select your player (Tag: Player, with a Rigidbody 2D). Add Component > Hide.
// 3. Press Play. Q hides, Q again comes out.
//
// Check Hold To Hide to stay hidden only while the key is held down.
// Your player turns see-through while hidden, so you can tell.
// To hide only in bushes or lockers, add HideSpot to them.

using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class Hide : MonoBehaviour
{
    [Tooltip("The key that hides you.")]
    public Key key = Key.Q;

    [Tooltip("Checked: hidden only while the key is held. Unchecked: press once to hide, again to come out.")]
    public bool holdToHide = false;

    // HideSpot keeps these. With no Hide Spots in the scene, you can hide anywhere.
    public static int spotsInScene = 0;
    public static int spotsTouching = 0;

    Rigidbody2D body;
    SpriteRenderer sprite;
    Behaviour sideMover;
    RigidbodyConstraints2D normalConstraints;
    Color normalColor;
    bool hidden = false;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        // Found by name, so Hide works whether or not SideMover.cs is in the project.
        sideMover = GetComponent("SideMover") as Behaviour;
    }

    void Update()
    {
        var keys = Keyboard.current;
        if (keys == null) return;

        bool wantHidden = hidden;
        if (holdToHide) wantHidden = keys[key].isPressed;
        else if (keys[key].wasPressedThisFrame) wantHidden = !hidden;

        if (wantHidden && !hidden && CanHideHere()) SetHidden(true);
        else if (!wantHidden && hidden) SetHidden(false);
    }

    bool CanHideHere()
    {
        return spotsInScene == 0 || spotsTouching > 0;
    }

    void SetHidden(bool on)
    {
        hidden = on;
        EnemyChase.playerHidden = on;

        if (on)
        {
            // Stop walking. In a platformer, keep falling so you land.
            normalConstraints = body.constraints;
            RigidbodyConstraints2D frozen = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
            if (body.gravityScale == 0f) frozen = frozen | RigidbodyConstraints2D.FreezePositionY;
            body.constraints = frozen;
        }
        else
        {
            body.constraints = normalConstraints;
        }

        // SideMover could still jump, so switch it off while hidden.
        if (sideMover != null) sideMover.enabled = !on;

        if (sprite != null)
        {
            if (on)
            {
                normalColor = sprite.color;
                Color seeThrough = normalColor;
                seeThrough.a = normalColor.a * 0.5f;
                sprite.color = seeThrough;
            }
            else
            {
                sprite.color = normalColor;
            }
        }
    }

    void OnDisable()
    {
        if (hidden) SetHidden(false);
    }
}
