// SellButton.cs 1.0.0
//
// A button that sells everything of one item for Gold.
//
// 1. Download Inventory.cs too. Put both anywhere in Assets/ (not in Assets/Editor/).
// 2. GameObject > UI (Canvas) > Legacy > Button. Change the button's text to Sell.
// 3. Select the button. Add Component > SellButton. Set Item and Gold Each.

using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SellButton : MonoBehaviour
{
    [Tooltip("What this button sells, like Ore.")]
    public string item = "Ore";

    [Tooltip("How much Gold you get for each one.")]
    public int goldEach = 5;

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(Sell);
    }

    void Sell()
    {
        int howMany = Inventory.Count(item);
        Inventory.Add(item, -howMany);
        Inventory.Add("Gold", howMany * goldEach);
    }
}
