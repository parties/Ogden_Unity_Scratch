// SelectFireTower.cs 1.0.0
//
// Click a tower to select it (it turns yellow). Then click a spot and only that
// tower fires there. It keeps firing where you click until you select another.
// Want every tower to fire at once? Use ClickFireTower instead.
//
// 1. Download ClickTools.cs, Shot.cs and Health.cs too. Put them all anywhere in
//    Assets/ (not in Assets/Editor/).
// 2. Make the shot (the steps are in Shot.cs).
// 3. Select each tower. Add Component > Box Collider 2D, then SelectFireTower.
//    Drag the shot prefab into Shot Prefab.
//
// This one uses both kinds of click. Clicking ON a tower is OnPointerDown.
// Clicking TOWARD a spot is checked in Update.

using UnityEngine;
using UnityEngine.EventSystems;

// Runs before Unity hands out clicks, so ClickIsTaken still sees a pickup that's
// about to disappear.
[DefaultExecutionOrder(-1001)]
public class SelectFireTower : MonoBehaviour, IPointerDownHandler
{
    [Tooltip("The shot prefab, with Shot on it.")]
    public Shot shotPrefab;

    [Tooltip("Seconds between shots. Smaller is faster.")]
    public float secondsBetweenShots = 1f;

    static SelectFireTower selected;

    float timer = 0f;

    void Start()
    {
        ClickTools.MakeClicksWork(gameObject);
    }

    // Unity calls this when this tower is clicked.
    public void OnPointerDown(PointerEventData eventData)
    {
        if (Time.timeScale == 0f) return; // the game is over
        if (selected != null) selected.Tint(Color.white);
        selected = this;
        Tint(Color.yellow);
    }

    void Update()
    {
        timer = timer + Time.deltaTime;
        if (selected != this) return;
        if (!ClickTools.LeftClicked() || ClickTools.ClickIsTaken()) return;
        if (timer < secondsBetweenShots) return;

        Shot shot = Instantiate(shotPrefab, transform.position, Quaternion.identity);
        shot.FlyToSpot(ClickTools.MouseInWorld());
        timer = 0f;
    }

    void Tint(Color color)
    {
        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        if (sprite != null) sprite.color = color;
    }
}
