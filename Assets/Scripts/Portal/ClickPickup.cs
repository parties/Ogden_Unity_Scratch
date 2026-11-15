// ClickPickup.cs 1.0.0
//
// Click it to collect it: ore, a coin, a gem on the ground.
//
// 1. Download ClickTools.cs and Inventory.cs too. Put all three anywhere in
//    Assets/ (not in Assets/Editor/).
// 2. Select the pickup. Add Component > Box Collider 2D, then ClickPickup.
// 3. Set Item to what it counts as, like Ore.
//
// Something on top is what gets the click. If the pickup won't click, raise its
// Order in Layer on the Sprite Renderer so it's above what's behind it.

using UnityEngine;
using UnityEngine.EventSystems;

public class ClickPickup : MonoBehaviour, IPointerDownHandler
{
    [Tooltip("What this counts as in the Inventory, like Ore or Coin.")]
    public string item = "Ore";

    [Tooltip("How many you get for one click.")]
    public int amount = 1;

    void Start()
    {
        ClickTools.MakeClicksWork(gameObject);
    }

    // Unity calls this when this object is clicked.
    public void OnPointerDown(PointerEventData eventData)
    {
        if (Time.timeScale == 0f) return; // the game is over
        Inventory.Add(item, amount);
        Destroy(gameObject);
    }
}
