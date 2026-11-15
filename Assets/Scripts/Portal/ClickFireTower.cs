// ClickFireTower.cs 1.0.0
//
// Click anywhere and every tower with this script fires at that spot.
// Want to pick one tower at a time? Use SelectFireTower instead.
//
// 1. Download ClickTools.cs, Shot.cs and Health.cs too. Put them all anywhere in
//    Assets/ (not in Assets/Editor/).
// 2. Make the shot (the steps are in Shot.cs).
// 3. Select each tower. Add Component > ClickFireTower. Drag the shot prefab
//    into Shot Prefab.
//
// Why every tower fires: each tower checks the mouse on its own in Update, so
// they all see the same click.

using UnityEngine;

// Runs before Unity hands out clicks, so ClickIsTaken still sees a pickup that's
// about to disappear.
[DefaultExecutionOrder(-1001)]
public class ClickFireTower : MonoBehaviour
{
    [Tooltip("The shot prefab, with Shot on it.")]
    public Shot shotPrefab;

    [Tooltip("Seconds between shots. Smaller is faster.")]
    public float secondsBetweenShots = 1f;

    float timer = 0f;

    void Update()
    {
        timer = timer + Time.deltaTime;
        if (!ClickTools.LeftClicked() || ClickTools.ClickIsTaken()) return;
        if (timer < secondsBetweenShots) return;

        Shot shot = Instantiate(shotPrefab, transform.position, Quaternion.identity);
        shot.FlyToSpot(ClickTools.MouseInWorld());
        timer = 0f;
    }
}
