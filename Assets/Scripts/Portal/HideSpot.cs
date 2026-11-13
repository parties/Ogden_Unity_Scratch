// HideSpot.cs 1.0.0
//
// A bush, locker or shadow the player can hide in. Once your scene has a Hide
// Spot, Hide only works while the player is standing in one.
//
// 1. Drop this file anywhere in Assets/ (not in Assets/Editor/).
//    Hide.cs and EnemyChase.cs have to be in your project too.
// 2. Select the bush. Add a Collider 2D (a Box Collider 2D works), then HideSpot.
//    HideSpot checks Is Trigger for you, so the player can walk into it.
// 3. Your player needs Tag: Player and Hide.

using UnityEngine;

public class HideSpot : MonoBehaviour
{
    bool playerInside = false;

    void Awake()
    {
        Collider2D zone = GetComponent<Collider2D>();
        if (zone != null) zone.isTrigger = true;
        else Debug.LogWarning(name + " needs a Collider 2D to be a Hide Spot.");
    }

    void OnEnable()
    {
        Hide.spotsInScene = Hide.spotsInScene + 1;
    }

    void OnDisable()
    {
        Hide.spotsInScene = Hide.spotsInScene - 1;
        if (playerInside) Leave();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !playerInside)
        {
            playerInside = true;
            Hide.spotsTouching = Hide.spotsTouching + 1;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && playerInside) Leave();
    }

    void Leave()
    {
        playerInside = false;
        Hide.spotsTouching = Hide.spotsTouching - 1;
    }
}
