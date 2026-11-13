// TowerBuyButton.cs 1.0.0
//
// One button in a tower menu. Click it to pay and build on the spot you clicked.
//
// 1. Download TowerSpot.cs, ClickTools.cs and Inventory.cs too. Put them all
//    anywhere in Assets/ (not in Assets/Editor/).
// 2. Make the menu: GameObject > UI (Canvas) > Panel. Right-click the Panel,
//    UI (Canvas) > Legacy > Button. Make one button per tower, and one to close.
// 3. On each tower button: Add Component > TowerBuyButton. Drag the tower prefab
//    into Tower Prefab and set Cost.
// 4. On the close button: Add Component > TowerBuyButton and leave Tower Prefab
//    empty.
// 5. Select each TowerSpot and drag the Panel into its Menu box.
// 6. Select the Panel and uncheck the box next to its name, so it starts hidden.
//
// To start with Gold: Add Component > Inventory on any object, set Start Gold.
// The menu sits on top of the map, so clicking a button can't click a spot too.

using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class TowerBuyButton : MonoBehaviour
{
    [Tooltip("The tower this button buys. Leave empty to make this the close button.")]
    public GameObject towerPrefab;

    [Tooltip("How much Gold it costs.")]
    public int cost = 10;

    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(Clicked);
    }

    void Clicked()
    {
        if (towerPrefab != null)
        {
            if (!Inventory.Spend("Gold", cost))
            {
                Debug.Log("Not enough Gold. This tower costs " + cost + ".");
                return;
            }
            TowerSpot.chosen.Build(towerPrefab);
        }

        // The button is inside the Panel, so its parent is the menu.
        transform.parent.gameObject.SetActive(false);
    }
}
