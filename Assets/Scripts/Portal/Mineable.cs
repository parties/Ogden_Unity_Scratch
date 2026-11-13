// Mineable.cs 1.0.0
//
// Click it a few times to break it and get something: a tree gives wood, a rock
// gives stone or drops ore on the ground.
//
// 1. Download ClickTools.cs and Inventory.cs too. Put all three anywhere in
//    Assets/ (not in Assets/Editor/).
// 2. Select the tree or rock. Add Component > Box Collider 2D, then Mineable.
// 3. Set Item, like Wood, and Hits To Break.
// 4. The player needs Tag: Player, so the Reach check knows where the player is.
//
// For a clicker game: set Reach to 0, uncheck Disappear When Broken, and drag a
// pickup prefab (with ClickPickup on it) into Drop Prefab.

using UnityEngine;
using UnityEngine.EventSystems;

public class Mineable : MonoBehaviour, IPointerDownHandler
{
    [Tooltip("What you get in the Inventory when it breaks, like Wood or Stone.")]
    public string item = "Wood";

    [Tooltip("How many you get when it breaks.")]
    public int amount = 1;

    [Tooltip("How many clicks it takes to break.")]
    public int hitsToBreak = 3;

    [Tooltip("How close the player has to be, in units. 0 means from anywhere.")]
    public float reach = 2f;

    [Tooltip("Check this to remove it when it breaks. Uncheck it to mine it again and again.")]
    public bool disappearWhenBroken = true;

    [Tooltip("A pickup to drop on the ground. Leave empty to go straight to the Inventory.")]
    public GameObject dropPrefab;

    int hitsLeft;
    GameObject player;

    void Start()
    {
        hitsLeft = hitsToBreak;
        player = GameObject.FindWithTag("Player");
        ClickTools.MakeClicksWork(gameObject);
    }

    // Unity calls this when this object is clicked.
    public void OnPointerDown(PointerEventData eventData)
    {
        if (Time.timeScale == 0f) return; // the game is over
        if (reach > 0f && player != null)
        {
            float distance = Vector2.Distance(transform.position, player.transform.position);
            if (distance > reach) return;
        }

        hitsLeft = hitsLeft - 1;
        Debug.Log(name + " hits left: " + hitsLeft);
        if (hitsLeft > 0) return;

        if (dropPrefab != null)
        {
            Vector2 nearby = (Vector2)transform.position + Random.insideUnitCircle.normalized;
            Instantiate(dropPrefab, nearby, Quaternion.identity);
        }
        else
        {
            Inventory.Add(item, amount);
        }

        hitsLeft = hitsToBreak;
        if (disappearWhenBroken) Destroy(gameObject);
    }
}
