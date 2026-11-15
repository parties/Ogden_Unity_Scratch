// AimShooter.cs 1.0.0
//
// Click anywhere and the player fires a shot toward the mouse.
//
// 1. Download ClickTools.cs, Shot.cs and Health.cs too. Put them all anywhere in
//    Assets/ (not in Assets/Editor/).
// 2. Make the shot (the steps are in Shot.cs).
// 3. Select the player. Add Component > AimShooter. Drag the shot prefab into
//    Shot Prefab.
//
// Clicking a menu button or a pickup doesn't fire. Clicking an enemy does.

using UnityEngine;

// Runs before Unity hands out clicks, so ClickIsTaken still sees a pickup that's
// about to disappear.
[DefaultExecutionOrder(-1001)]
public class AimShooter : MonoBehaviour
{
    [Tooltip("The shot prefab, with Shot on it.")]
    public Shot shotPrefab;

    [Tooltip("Seconds between shots. Smaller is faster.")]
    public float secondsBetweenShots = 0.25f;

    float timer = 0f;

    void Update()
    {
        timer = timer + Time.deltaTime;
        if (!ClickTools.LeftClicked() || ClickTools.ClickIsTaken()) return;
        if (timer < secondsBetweenShots) return;

        Shot shot = Instantiate(shotPrefab, transform.position, Quaternion.identity);
        shot.FlyToward(ClickTools.MouseInWorld());
        timer = 0f;
    }
}
