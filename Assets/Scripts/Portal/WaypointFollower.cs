// WaypointFollower.cs 1.0.0
//
// An enemy walks a path, point to point, like in a tower defense game.
//
// 1. Drop this file anywhere in Assets/ (not in Assets/Editor/).
// 2. Make the path: GameObject > Create Empty, name it Path. Right-click Path,
//    Create Empty, and drag the child to the first corner of the path. Make one
//    child per corner. The enemy visits them in Hierarchy order, top to bottom.
// 3. Select the enemy. Add Component > WaypointFollower. Drag Path from the
//    Hierarchy into the Path box.
// 4. At the end of the path the enemy hurts the base (drag the base, which
//    needs a Health component, into Base Health) and disappears. Leave Base
//    Health empty and reaching the end is "You lose!" right away.
//
// More enemies: select the enemy, Ctrl+D (Cmd+D on a Mac), and drag the copy
// back along the path before the first point so they don't walk stacked.

using UnityEngine;

public class WaypointFollower : MonoBehaviour
{
    [Tooltip("The empty object whose children are the points of the path.")]
    public Transform path;

    [Tooltip("How fast the enemy walks, in units per second.")]
    public float speed = 2f;

    [Tooltip("The base's Health. Leave empty to lose as soon as an enemy reaches the end.")]
    public Health baseHealth;

    [Tooltip("How many HP the base loses when this enemy reaches the end.")]
    public int damageAtEnd = 1;

    [Tooltip("Uncheck for a friendly troop. It walks the path and disappears at the end without hurting anything.")]
    public bool isEnemy = true;

    int next = 0;

    void Update()
    {
        if (path == null || path.childCount == 0) return;

        Transform point = path.GetChild(next);
        transform.position = Vector3.MoveTowards(transform.position, point.position, speed * Time.deltaTime);
        if (transform.position != point.position) return;

        next = next + 1;
        if (next < path.childCount) return;

        if (!isEnemy)
        {
            Destroy(gameObject);
            return;
        }

        if (baseHealth != null)
        {
            baseHealth.TakeDamage(damageAtEnd);
        }
        else
        {
            Debug.Log("You lose!");
            Time.timeScale = 0f;
        }
        // This enemy is done. If it was the last one and the base is still
        // standing, you win.
        Health mine = GetComponent<Health>();
        if (mine != null) mine.hp = 0;
        if (Time.timeScale > 0f && Health.winWhenEnemiesGone && !Health.EnemiesLeft())
        {
            Debug.Log("You win!");
            Time.timeScale = 0f;
        }
        Destroy(gameObject);
    }
}
