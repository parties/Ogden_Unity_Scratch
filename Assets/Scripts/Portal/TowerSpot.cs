// TowerSpot.cs 1.0.0
//
// A place on the map that builds a tower when you click it. Each spot holds one
// tower.
//
// 1. Download ClickTools.cs too. Put both anywhere in Assets/ (not in Assets/Editor/).
// 2. Make a spot: GameObject > 2D Object > Sprites > Square. Add Component >
//    Box Collider 2D, then TowerSpot.
// 3. Make your tower a prefab: drag it from the Hierarchy into the Project window.
// 4. Free tower: drag the prefab into Tower Prefab. Done.
//    Buy menu instead: leave Tower Prefab empty and see TowerBuyButton.cs.
// 5. More spots: select the spot and press Ctrl+D (Cmd+D on a Mac).
//
// Make sure nothing with a collider sits on top of a spot. The top object gets
// the click.

using UnityEngine;
using UnityEngine.EventSystems;

public class TowerSpot : MonoBehaviour, IPointerDownHandler
{
    [Tooltip("The tower to build for free. Leave empty to open the menu instead.")]
    public GameObject towerPrefab;

    [Tooltip("The menu panel to open when this spot is clicked. Leave empty for a free tower.")]
    public GameObject menu;

    // The spot that was clicked last. TowerBuyButton builds the tower here.
    public static TowerSpot chosen;

    GameObject tower;

    void Start()
    {
        ClickTools.MakeClicksWork(gameObject);
    }

    // Unity calls this when this object is clicked.
    public void OnPointerDown(PointerEventData eventData)
    {
        if (Time.timeScale == 0f) return; // the game is over
        if (tower != null) return;

        if (towerPrefab != null)
        {
            Build(towerPrefab);
        }
        else if (menu != null)
        {
            chosen = this;
            menu.SetActive(true);
        }
    }

    public void Build(GameObject prefab)
    {
        tower = Instantiate(prefab, transform.position, Quaternion.identity);
    }
}
