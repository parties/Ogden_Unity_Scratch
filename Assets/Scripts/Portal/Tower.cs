// Tower.cs 1.0.0
//
// Hits the nearest enemy in range, over and over. No bullets: the hit lands
// right away. Also works as an auto-attack on a top-down fighter's player.
//
// 1. Drop this file anywhere in Assets/ (not in Assets/Editor/).
// 2. Add the tag Enemy: at the top of the Inspector, Tag > Add Tag..., +,
//    type Enemy, Save. Set Tag: Enemy on every enemy. Each enemy needs Health.
// 3. Select the tower. Add Component > Tower.
// 4. With the tower selected, a yellow circle in the Scene view shows its
//    range. A red line flashes in the Scene view each time it hits.
//
// Place towers by dragging copies onto the map before you press Play.

using UnityEngine;

public class Tower : MonoBehaviour
{
    [Tooltip("How far the tower reaches, in units. The yellow circle in the Scene view.")]
    public float range = 3f;

    [Tooltip("How many HP one hit takes away.")]
    public int damage = 1;

    [Tooltip("Seconds between hits. Smaller is faster.")]
    public float secondsBetweenHits = 1f;

    float timer = 0f;

    void Update()
    {
        timer = timer + Time.deltaTime;
        if (timer < secondsBetweenHits) return;

        Health target = NearestEnemy();
        if (target == null) return;

        Debug.DrawLine(transform.position, target.transform.position, Color.red, 0.2f);
        target.TakeDamage(damage);
        timer = 0f;
    }

    // ponytail: searches every enemy each hit; fine for dozens, keep a list if there are hundreds.
    Health NearestEnemy()
    {
        Health nearest = null;
        float closest = range;
        foreach (GameObject enemy in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            Health health = enemy.GetComponent<Health>();
            float distance = Vector2.Distance(transform.position, enemy.transform.position);
            if (health != null && health.hp > 0 && distance <= closest)
            {
                nearest = health;
                closest = distance;
            }
        }
        return nearest;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
