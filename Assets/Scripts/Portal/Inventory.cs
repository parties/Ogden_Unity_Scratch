// Inventory.cs 1.0.0
//
// Counts what the player has (Gold, Wood, Ore, anything) and shows it on screen.
//
// 1. Drop this file anywhere in Assets/ (not in Assets/Editor/).
// 2. Nothing else is needed. If no object has an Inventory when something is
//    collected, one is made for you. To start with gold: GameObject > Create
//    Empty, Add Component > Inventory, set Start Gold.
//
// In your own scripts:
//   Inventory.Add("Wood", 1);
//   Inventory.Count("Wood")
//   if (Inventory.Spend("Gold", 10)) { /* you paid */ }

using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [Tooltip("How much Gold the player starts with.")]
    public int startGold = 0;

    Dictionary<string, int> items = new Dictionary<string, int>();

    static Inventory instance;

    void Awake()
    {
        instance = this;
        if (startGold > 0) items["Gold"] = startGold;
    }

    static Inventory Get()
    {
        if (instance == null)
        {
            new GameObject("Inventory").AddComponent<Inventory>();
        }
        return instance;
    }

    public static int Count(string item)
    {
        int count;
        Get().items.TryGetValue(item, out count);
        return count;
    }

    public static void Add(string item, int amount)
    {
        int newCount = Count(item) + amount;
        Get().items[item] = newCount;
    }

    // Takes the amount and returns true, or takes nothing and returns false.
    public static bool Spend(string item, int amount)
    {
        if (Count(item) < amount) return false;

        Add(item, -amount);
        return true;
    }

    void OnGUI()
    {
        GUI.skin.label.fontSize = 22;
        float y = 10f;
        foreach (KeyValuePair<string, int> pair in items)
        {
            GUI.Label(new Rect(10f, y, 400f, 34f), pair.Key + ": " + pair.Value);
            y = y + 30f;
        }
    }
}
