// ArcherTower.cs 1.0.0
//
// Fires a shot you can see at the nearest enemy in range, by itself. Tower hits
// right away with no bullet. This one sends an arrow.
//
// 1. Download Shot.cs and Health.cs too. Put all three anywhere in Assets/ (not in
//    Assets/Editor/).
// 2. Make the arrow (the steps are in Shot.cs). On Shot, Damage is what one
//    arrow takes away.
// 3. Add the tag Enemy. Set Tag: Enemy on every enemy. Each enemy needs Health.
// 4. Select the tower. Add Component > ArcherTower. Drag the arrow prefab into
//    Shot Prefab.
// 5. With the tower selected, a yellow circle in the Scene view shows its range.

using UnityEngine;

public class ArcherTower : MonoBehaviour
{
    [Tooltip("The arrow prefab, with Shot on it.")]
    public Shot shotPrefab;

    [Tooltip("How far the tower reaches, in units. The yellow circle in the Scene view.")]
    public float range = 3f;

    [Tooltip("Seconds between shots. Smaller is faster.")]
    public float secondsBetweenShots = 1f;

    float timer = 0f;

    void Update()
    {
        timer = timer + Time.deltaTime;
        if (timer < secondsBetweenShots) return;

        Health target = NearestEnemy();
        if (target == null) return;

        Shot shot = Instantiate(shotPrefab, transform.position, Quaternion.identity);
        shot.FlyAtTarget(target.transform);
        timer = 0f;
    }

    // ponytail: searches every enemy each shot; fine for dozens, keep a list if there are hundreds.
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
