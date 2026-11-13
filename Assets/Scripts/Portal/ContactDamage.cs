// ContactDamage.cs 1.0.0
//
// Hurts what it touches: spikes, an enemy that hits the player, a player that
// hits an enemy, or an enemy that reaches the base.
//
// 1. Drop this file anywhere in Assets/ (not in Assets/Editor/).
// 2. Select the thing that does the hurting. It needs a Collider 2D.
//    Add Component > ContactDamage.
// 3. Target Tag is the tag of what gets hurt (Player, Enemy, or your base's
//    tag). That object needs a Health component.
// 4. One of the two objects needs a Rigidbody 2D, or Unity never notices the
//    touch. A player with a mover already has one.
//
// Works with Is Trigger checked (passes through) or unchecked (bumps).
// It hurts once per touch: move away and touch again to hurt again. If what it
// hurts has Safe Time above 0 on its Health, staying in touch hurts again each
// time Safe Time runs out.

using UnityEngine;

public class ContactDamage : MonoBehaviour
{
    [Tooltip("How many HP one touch takes away.")]
    public int damage = 1;

    [Tooltip("The tag of what this hurts: Player, Enemy, or your base's tag.")]
    public string targetTag = "Player";

    [Tooltip("Check this to make this object disappear after it hurts something (an enemy that reaches the base).")]
    public bool destroySelfOnHit = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        Hit(other.gameObject, false);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        Hit(collision.gameObject, false);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        Hit(other.gameObject, true);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        Hit(collision.gameObject, true);
    }

    void Hit(GameObject other, bool stillTouching)
    {
        if (other.tag != targetTag) return;

        Health health = other.GetComponent<Health>();
        if (health == null) return;
        // With no Safe Time, staying in touch would hurt every physics step.
        if (stillTouching && health.safeTime <= 0f) return;

        health.TakeDamage(damage);
        if (destroySelfOnHit) Destroy(gameObject);
    }
}
