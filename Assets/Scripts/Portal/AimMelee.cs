// AimMelee.cs 1.0.0
//
// Click anywhere and the player swings toward the mouse. Everything with the
// right tag inside the swing gets hurt.
//
// 1. Download ClickTools.cs and Health.cs too. Put all three anywhere in Assets/
//    (not in Assets/Editor/).
// 2. Select the player. Add Component > AimMelee.
// 3. Each enemy needs Tag: Enemy, Health and a Collider 2D.
// 4. To see the swing, drag a sprite prefab into Swing Prefab. With the player
//    selected, a yellow circle in the Scene view shows the last swing.

using UnityEngine;

// Runs before Unity hands out clicks, so ClickIsTaken still sees a pickup that's
// about to disappear.
[DefaultExecutionOrder(-1001)]
public class AimMelee : MonoBehaviour
{
    [Tooltip("How many HP one swing takes away.")]
    public int damage = 1;

    [Tooltip("How far from the player the swing lands, in units.")]
    public float reach = 1.5f;

    [Tooltip("How big the swing is, in units.")]
    public float swingSize = 1f;

    [Tooltip("Seconds between swings. Smaller is faster.")]
    public float secondsBetweenSwings = 0.4f;

    [Tooltip("The tag of what this hurts.")]
    public string targetTag = "Enemy";

    [Tooltip("A sprite to show for a moment where the swing lands. Can be empty.")]
    public GameObject swingPrefab;

    float timer = 0f;
    Vector3 lastSwing;

    void Update()
    {
        timer = timer + Time.deltaTime;
        if (!ClickTools.LeftClicked() || ClickTools.ClickIsTaken()) return;
        if (timer < secondsBetweenSwings) return;

        Vector3 direction = (ClickTools.MouseInWorld() - transform.position).normalized;
        lastSwing = transform.position + direction * reach;
        timer = 0f;

        foreach (Collider2D thing in Physics2D.OverlapCircleAll(lastSwing, swingSize))
        {
            Health health = thing.GetComponent<Health>();
            if (health != null && thing.tag == targetTag) health.TakeDamage(damage);
        }

        if (swingPrefab != null)
        {
            Destroy(Instantiate(swingPrefab, lastSwing, Quaternion.identity), 0.15f);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(lastSwing, swingSize);
    }
}
