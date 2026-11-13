// Crosshair.cs 1.0.0
//
// A sprite that follows the mouse, in place of the normal cursor.
//
// 1. Download ClickTools.cs too. Put both anywhere in Assets/ (not in Assets/Editor/).
// 2. Add your crosshair sprite to the scene. Don't give it a collider: it would
//    block clicks on what's under it.
// 3. Add Component > Crosshair. On the Sprite Renderer, raise Order in Layer so
//    it draws on top of everything.

using UnityEngine;
using UnityEngine.InputSystem;

public class Crosshair : MonoBehaviour
{
    void OnEnable()
    {
        Cursor.visible = false;
    }

    void OnDisable()
    {
        Cursor.visible = true;
    }

    void Update()
    {
        if (Mouse.current == null) return;

        transform.position = ClickTools.MouseInWorld();
    }
}
