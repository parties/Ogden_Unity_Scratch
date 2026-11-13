// EnemyChase.cs 1.1.0
//
// An enemy for a top-down game. When it sees the player, it chases.
//
// 1. Drop this file anywhere in Assets/ (not in Assets/Editor/).
// 2. The player needs Tag: Player.
// 3. Select the enemy. Add Component > Rigidbody 2D, a Collider 2D, then EnemyChase.
//
// Vision Angle 360 sees all the way around. A smaller number is a vision cone,
// like 90. With the enemy selected, yellow lines in the Scene view show what it
// can see. It turns toward the player while chasing, so it only loses you when
// you get past Sight Range or behind a wall.
//
// A wall is anything with a collider that doesn't move: a Tilemap Collider 2D,
// or a Box Collider 2D with no Rigidbody 2D. Other enemies and the player don't
// block sight. Uncheck Walls Block Sight to see through everything.
//
// To let the player hide, add Hide to the player.
// For a platformer, use PlatformChase instead.
// To hurt the player, add ContactDamage and Health.

using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyChase : MonoBehaviour
{
    [Tooltip("How fast it chases, in units per second.")]
    public float speed = 2f;

    [Tooltip("How far it can see, in units.")]
    public float sightRange = 8f;

    [Tooltip("How wide it can see, in degrees. 360 is all the way around.")]
    [Range(1f, 360f)]
    public float visionAngle = 360f;

    [Tooltip("The way it looks before it has seen the player.")]
    public Vector2 facing = Vector2.right;

    [Tooltip("Walls block what it can see. Uncheck to see through walls.")]
    public bool wallsBlockSight = true;

    [Tooltip("If it was chasing when the player hid, it keeps chasing this many seconds.")]
    public float keepChasingAfterHide = 3f;

    // While this is true, no enemy can see the player. Hide sets it.
    public static bool playerHidden = false;

    Rigidbody2D body;
    Transform player;
    float chaseUntil = 0f;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
    }

    void Start()
    {
        GameObject found = GameObject.FindWithTag("Player");
        if (found != null) player = found.transform;
        else Debug.LogWarning(name + " can't find the player. Set Tag: Player on your player.");
    }

    void FixedUpdate()
    {
        if (player != null && ShouldChase())
        {
            facing = (player.position - transform.position).normalized;
            body.linearVelocity = facing * speed;
        }
        else
        {
            body.linearVelocity = Vector2.zero;
        }
    }

    bool ShouldChase()
    {
        // Hidden: only an enemy that was already chasing keeps going, for a little while.
        if (playerHidden) return Time.time < chaseUntil;

        if (CanSee(transform.position, facing, player.position, sightRange, visionAngle, wallsBlockSight))
        {
            chaseUntil = Time.time + keepChasingAfterHide;
            return true;
        }
        chaseUntil = 0f;
        return false;
    }

    // True when an enemy at "from", looking along "look", can see "target".
    // PlatformChase uses this too.
    public static bool CanSee(Vector2 from, Vector2 look, Vector2 target, float range, float angle, bool wallsBlock)
    {
        Vector2 toTarget = target - from;
        if (toTarget.magnitude > range) return false;
        if (Vector2.Angle(look, toTarget) > angle / 2f) return false;
        if (wallsBlock && WallBetween(from, target)) return false;
        return true;
    }

    public static bool WallBetween(Vector2 from, Vector2 to)
    {
        foreach (RaycastHit2D hit in Physics2D.LinecastAll(from, to))
        {
            if (IsWall(hit.collider)) return true;
        }
        return false;
    }

    // A wall is a solid collider that doesn't move: no Rigidbody 2D, or a Static one.
    // That leaves out the player, enemies and shots, which all have a moving body.
    public static bool IsWall(Collider2D collider)
    {
        if (collider.isTrigger) return false;
        Rigidbody2D wallBody = collider.attachedRigidbody;
        return wallBody == null || wallBody.bodyType == RigidbodyType2D.Static;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 from = transform.position;
        Vector3 left = Quaternion.Euler(0f, 0f, visionAngle / 2f) * facing.normalized * sightRange;
        Vector3 right = Quaternion.Euler(0f, 0f, -visionAngle / 2f) * facing.normalized * sightRange;
        Gizmos.DrawLine(from, from + left);
        Gizmos.DrawLine(from, from + right);
        Gizmos.DrawWireSphere(from, sightRange);
    }
}
