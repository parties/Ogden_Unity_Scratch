// TowerPlacer.cs 1.0.0
//
// Press a key and a tower sticks to your mouse. Click anywhere to put it down.
// Press Esc to cancel.
//
// 1. Download ClickTools.cs too. Put both anywhere in Assets/ (not in Assets/Editor/).
// 2. Make your tower a prefab: drag it from the Hierarchy into the Project window.
// 3. GameObject > Create Empty, name it GameManager. Add Component > TowerPlacer.
// 4. Drag the tower prefab into Tower Prefab. Pick a key in Pick Up Key.
//
// Want it to fire only after it's placed? It already does: every script on the
// tower is turned off while you're carrying it.

using UnityEngine;
using UnityEngine.InputSystem;

// Runs before Unity hands out clicks, so ClickIsTaken still sees a pickup that's
// about to disappear.
[DefaultExecutionOrder(-1001)]
public class TowerPlacer : MonoBehaviour
{
    [Tooltip("The tower to place.")]
    public GameObject towerPrefab;

    [Tooltip("The key that picks up a new tower.")]
    public Key pickUpKey = Key.T;

    GameObject held;

    void Update()
    {
        Keyboard keys = Keyboard.current;
        if (keys == null || Mouse.current == null) return;

        if (held == null)
        {
            if (keys[pickUpKey].wasPressedThisFrame)
            {
                held = Instantiate(towerPrefab, ClickTools.MouseInWorld(), Quaternion.identity);
                TurnScripts(held, false);
            }
            return;
        }

        held.transform.position = ClickTools.MouseInWorld();

        if (keys.escapeKey.wasPressedThisFrame)
        {
            Destroy(held);
        }
        else if (ClickTools.LeftClicked() && !ClickTools.ClickIsTaken())
        {
            TurnScripts(held, true);
            held = null;
        }
    }

    void TurnScripts(GameObject tower, bool on)
    {
        foreach (MonoBehaviour script in tower.GetComponents<MonoBehaviour>())
        {
            script.enabled = on;
        }
    }
}
