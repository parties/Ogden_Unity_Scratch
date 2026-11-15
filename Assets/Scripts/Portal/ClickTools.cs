// ClickTools.cs 1.0.0
//
// Helpers the click scripts share. Keep this file anywhere in Assets/ (not in
// Assets/Editor/). You don't add it to an object.
//
// There are two ways to read a mouse click, and the scripts use both:
//
//   Clicking ON something (a pickup, a tower spot): the object handles its own
//   click with OnPointerDown. Only the top object under the mouse gets it.
//
//   Clicking TOWARD something (aiming a shot at a spot): the script checks the
//   mouse every frame in Update. Every script that checks sees the same click.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class ClickTools
{
    // OnPointerDown needs an EventSystem in the scene and a Physics 2D Raycaster
    // on the camera. This adds them if they're missing and says so in the Console.
    public static void MakeClicksWork(GameObject clickable)
    {
        if (clickable.GetComponent<Collider2D>() == null)
        {
            Debug.LogWarning(clickable.name + " needs a Collider 2D, or clicks can't reach it.");
        }

        if (EventSystem.current == null)
        {
            GameObject system = new GameObject("EventSystem");
            system.AddComponent<EventSystem>();
            system.AddComponent<InputSystemUIInputModule>();
            Debug.Log("Click Tools added an EventSystem to this scene.");
        }

        Camera camera = Camera.main;
        if (camera != null && camera.GetComponent<Physics2DRaycaster>() == null)
        {
            camera.gameObject.AddComponent<Physics2DRaycaster>();
            Debug.Log("Click Tools added a Physics 2D Raycaster to the camera.");
        }
    }

    // True on the one frame the left button goes down.
    public static bool LeftClicked()
    {
        Mouse mouse = Mouse.current;
        return mouse != null && Time.timeScale > 0f && mouse.leftButton.wasPressedThisFrame;
    }

    // Where the mouse is in the game world.
    public static Vector3 MouseInWorld()
    {
        Vector3 spot = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        spot.z = 0f;
        return spot;
    }

    // True if the mouse is over a menu or over something that handles its own
    // click. Aiming scripts check this so a click on a button doesn't also fire.
    // Clicking on an enemy is not taken: it has no click handler of its own.
    // A script that uses this needs [DefaultExecutionOrder(-1001)] above its class.
    public static bool ClickIsTaken()
    {
        if (EventSystem.current == null) return false;

        PointerEventData pointer = new PointerEventData(EventSystem.current);
        pointer.position = Mouse.current.position.ReadValue();
        List<RaycastResult> hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, hits);
        if (hits.Count == 0) return false;

        // The list is sorted, so the first hit is the top object under the mouse.
        RaycastResult top = hits[0];
        return top.module is GraphicRaycaster
            || ExecuteEvents.GetEventHandler<IPointerDownHandler>(top.gameObject) != null;
    }
}
