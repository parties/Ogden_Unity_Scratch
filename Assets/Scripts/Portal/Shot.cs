// Shot.cs 1.0.0
//
// A bullet, arrow or fireball. It flies, hurts the first thing it touches with
// the right tag, and disappears. Aim Shooter, Archer Tower, Click Fire Tower and
// Select Fire Tower all fire this.
//
// 1. Download Health.cs too. Put both anywhere in Assets/ (not in Assets/Editor/).
// 2. Make a small sprite that faces right. Add Component > Shot. It sets up its
//    own Rigidbody 2D and collider.
// 3. Make it a prefab: drag it from the Hierarchy into the Project window. Then
//    delete it from the Hierarchy.
// 4. Whatever it hits needs Health, a Collider 2D and the tag in Target Tag.

using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Shot : MonoBehaviour
{
    [Tooltip("How fast it flies, in units per second.")]
    public float speed = 10f;

    [Tooltip("How many HP it takes away.")]
    public int damage = 1;

    [Tooltip("The tag of what it hurts: Enemy, Player, or your own tag.")]
    public string targetTag = "Enemy";

    [Tooltip("It disappears after this many seconds if it hasn't hit anything.")]
    public float secondsToLive = 3f;

    Vector3 direction;     // used by FlyToward
    Vector3 spot;          // used by FlyToSpot and FlyAtTarget
    bool stopAtSpot;
    Transform target;
    bool used = false;     // a shot only hurts once

    void Awake()
    {
        Rigidbody2D body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        // Without this a Kinematic body never touches walls.
        body.useFullKinematicContacts = true;

        Collider2D shape = GetComponent<Collider2D>();
        if (shape == null) shape = gameObject.AddComponent<CircleCollider2D>();
        shape.isTrigger = true;

        Destroy(gameObject, secondsToLive);
    }

    // Flies through the point and keeps going.
    public void FlyToward(Vector3 point)
    {
        direction = (point - transform.position).normalized;
        transform.right = direction;
    }

    // Flies to the point and disappears there.
    public void FlyToSpot(Vector3 point)
    {
        spot = point;
        stopAtSpot = true;
        transform.right = point - transform.position;
    }

    // Follows a moving target until it hits.
    public void FlyAtTarget(Transform enemy)
    {
        target = enemy;
        FlyToSpot(enemy.position);
    }

    void Update()
    {
        if (!stopAtSpot)
        {
            Vector3 next = transform.position + direction * speed * Time.deltaTime;
            // Look along the step first, so a wall stops it even when it's thin.
            foreach (RaycastHit2D hit in Physics2D.LinecastAll(transform.position, next))
            {
                if (IsWall(hit.collider)) { Destroy(gameObject); return; }
            }
            transform.position = next;
            return;
        }

        if (target != null)
        {
            spot = target.position;
            transform.right = spot - transform.position;
        }

        transform.position = Vector3.MoveTowards(transform.position, spot, speed * Time.deltaTime);
        if (transform.position != spot) return;

        // A fast shot can land between physics steps, so check what's at the spot.
        if (target != null) Hit(target.gameObject);
        foreach (Collider2D there in Physics2D.OverlapPointAll(spot)) Hit(there.gameObject);
        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Hit(other.gameObject);
    }

    // A wall is a solid collider that doesn't move.
    bool IsWall(Collider2D other)
    {
        return !other.isTrigger && (other.attachedRigidbody == null || other.attachedRigidbody.bodyType == RigidbodyType2D.Static);
    }

    void Hit(GameObject other)
    {
        if (used || other.tag != targetTag) return;

        Health health = other.GetComponent<Health>();
        if (health == null) return;

        used = true;
        health.TakeDamage(damage);
        Destroy(gameObject);
    }
}
