// PlatformChase.cs 1.0.0
//
// An enemy for a platformer. It walks back and forth on its platform, and when
// it sees the player it runs at them. It stops at ledges instead of falling off.
//
// 1. Drop this file anywhere in Assets/ (not in Assets/Editor/).
//    EnemyChase.cs has to be in your project too. Don't add EnemyChase to this
//    enemy; PlatformChase only borrows its sight code.
// 2. The player needs Tag: Player.
// 3. Select the enemy. Add Component > Rigidbody 2D, a Collider 2D (Box or
//    Capsule), then PlatformChase.
// 4. Set up the ground the way the Tilemap Lab does: Tilemap Collider 2D with
//    Composite Operation = Merge, a Composite Collider 2D, and Body Type =
//    Static on the Rigidbody 2D the composite adds. With a Tilemap Collider 2D
//    alone, the enemy catches on the lines between tiles and stops.
//
// Check Can Jump and it jumps over walls and gaps, and up toward a player
// above it, instead of stopping.
// It flips to face the way it walks. Kenney enemies are drawn facing left, so
// Sprite Faces Left starts checked. Uncheck it if your enemy faces right.
// With the enemy selected, yellow lines in the Scene view show what it can see.
// Walls block sight, like EnemyChase. Hide on the player works here too.
// When it loses sight of you (a wall, or you hide), it keeps chasing for Keep
// Chasing After Hide seconds, so a jumper follows you over a wall.
// To hurt the player, add ContactDamage and Health.

using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlatformChase : MonoBehaviour
{
    [Tooltip("How fast it runs at the player, in units per second.")]
    public float chaseSpeed = 3f;

    [Tooltip("Walk back and forth when it can't see the player. Unchecked, it stands still.")]
    public bool patrol = true;

    [Tooltip("How fast it walks while patrolling, in units per second.")]
    public float patrolSpeed = 1f;

    [Tooltip("How far it can see, in units.")]
    public float sightRange = 8f;

    [Tooltip("How wide it can see, in degrees, in the direction it faces. 360 is all the way around.")]
    [Range(1f, 360f)]
    public float visionAngle = 90f;

    [Tooltip("Start facing right. Uncheck to start facing left.")]
    public bool startFacingRight = true;

    [Tooltip("Kenney enemies are drawn facing left. Uncheck if your enemy is drawn facing right.")]
    public bool spriteFacesLeft = true;

    [Tooltip("Walls block what it can see. Uncheck to see through walls.")]
    public bool wallsBlockSight = true;

    [Tooltip("If it was chasing when it lost sight of the player (a wall, or the player hid), it keeps chasing this many seconds.")]
    public float keepChasingAfterHide = 3f;

    [Tooltip("Jump over walls and gaps, and up toward the player, instead of stopping.")]
    public bool canJump = false;

    [Tooltip("How fast it leaves the ground when it jumps. Bigger means a higher jump.")]
    public float jumpSpeed = 10f;

    [Tooltip("How strongly it is pulled down. 3 matches SideMover.")]
    public float gravity = 3f;

    Rigidbody2D body;
    Collider2D box;
    SpriteRenderer sprite;
    Transform player;
    ContactFilter2D groundFilter;
    float direction = 1f;
    float chaseUntil = 0f;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        box = GetComponent<Collider2D>();
        sprite = GetComponent<SpriteRenderer>();
        body.freezeRotation = true;
        if (!startFacingRight) direction = -1f;

        // Ground is anything we touch from above, the same test SideMover uses.
        groundFilter.useNormalAngle = true;
        groundFilter.minNormalAngle = 45f;
        groundFilter.maxNormalAngle = 135f;
    }

    void Start()
    {
        GameObject found = GameObject.FindWithTag("Player");
        if (found != null) player = found.transform;
        else Debug.LogWarning(name + " can't find the player. Set Tag: Player on your player.");
        if (box == null) Debug.LogWarning(name + " needs a Collider 2D for PlatformChase to find ledges.");
    }

    void FixedUpdate()
    {
        body.gravityScale = gravity;
        Vector2 velocity = body.linearVelocity;
        bool chasing = player != null && ShouldChase();

        float speed = 0f;
        if (chasing)
        {
            float gap = player.position.x - transform.position.x;
            if (Mathf.Abs(gap) > 0.1f)
            {
                direction = Mathf.Sign(gap);
                speed = chaseSpeed;
            }
        }
        else if (patrol)
        {
            speed = patrolSpeed;
        }

        if (box == null)
        {
            // No collider, so no ledge or wall checks.
        }
        else if (!IsGrounded())
        {
            // In the air, pushing into a wall makes it stick, so wait until the feet clear it.
            if (WallAtFeet()) speed = 0f;
        }
        else
        {
            bool blocked = WallAhead(0.1f) || LedgeAhead();
            bool playerAbove = chasing && player.position.y > box.bounds.max.y;
            // A jumper takes off a tile early, so it isn't stuck against the wall going up.
            bool wallSoon = WallAhead(1f);

            if (canJump && chasing && (blocked || wallSoon || playerAbove))
            {
                velocity.y = jumpSpeed;
            }
            else if (blocked)
            {
                if (chasing) speed = 0f;     // wait at the edge
                else direction = -direction; // patrol turns around
            }
        }

        velocity.x = direction * speed;
        body.linearVelocity = velocity;

        // Flip when it walks the opposite way from how the art is drawn.
        if (sprite != null) sprite.flipX = (direction < 0f) != spriteFacesLeft;
    }

    bool ShouldChase()
    {
        Vector2 look = new Vector2(direction, 0f);
        if (!EnemyChase.playerHidden && EnemyChase.CanSee(transform.position, look, player.position, sightRange, visionAngle, wallsBlockSight))
        {
            chaseUntil = Time.time + keepChasingAfterHide;
            return true;
        }
        // Lost sight (a wall, or the player hid): keep chasing a little while, so a
        // jumper follows the player over a wall instead of turning back.
        return Time.time < chaseUntil;
    }

    bool IsGrounded()
    {
        return body.IsTouching(groundFilter);
    }

    // A wall within "reach" units in front, at waist height.
    bool WallAhead(float reach)
    {
        Bounds b = box.bounds;
        Vector2 from = new Vector2(b.center.x, b.center.y);
        return HitsWall(from, new Vector2(direction, 0f), b.extents.x + reach);
    }

    // A wall touching the front, down at foot height.
    bool WallAtFeet()
    {
        Bounds b = box.bounds;
        Vector2 foot = new Vector2(b.center.x, b.min.y + 0.05f);
        return HitsWall(foot, new Vector2(direction, 0f), b.extents.x + 0.05f);
    }

    // No ground just past the front foot.
    bool LedgeAhead()
    {
        Bounds b = box.bounds;
        Vector2 frontFoot = new Vector2(b.center.x + direction * (b.extents.x + 0.1f), b.min.y + 0.1f);
        return !HitsWall(frontFoot, Vector2.down, 0.5f);
    }

    static bool HitsWall(Vector2 from, Vector2 way, float distance)
    {
        foreach (RaycastHit2D hit in Physics2D.RaycastAll(from, way, distance))
        {
            if (EnemyChase.IsWall(hit.collider)) return true;
        }
        return false;
    }

    void OnDrawGizmosSelected()
    {
        float facing = direction;
        if (!Application.isPlaying) facing = startFacingRight ? 1f : -1f;
        Vector3 look = new Vector3(facing, 0f, 0f);

        Gizmos.color = Color.yellow;
        Vector3 from = transform.position;
        Vector3 up = Quaternion.Euler(0f, 0f, visionAngle / 2f) * look * sightRange;
        Vector3 down = Quaternion.Euler(0f, 0f, -visionAngle / 2f) * look * sightRange;
        Gizmos.DrawLine(from, from + up);
        Gizmos.DrawLine(from, from + down);
        Gizmos.DrawWireSphere(from, sightRange);
    }
}
