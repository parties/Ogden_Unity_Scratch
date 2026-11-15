// TroopGold.cs 1.0.0
//
// The farther a troop walks, the more Gold you make.
//
// 1. Download Inventory.cs too. Put both anywhere in Assets/ (not in Assets/Editor/).
// 2. Select the troop. Add Component > TroopGold. WaypointFollower can walk it
//    along a path, or use any other mover.

using UnityEngine;

public class TroopGold : MonoBehaviour
{
    [Tooltip("How many units it walks to earn one Gold.")]
    public float unitsPerGold = 1f;

    Vector3 lastPosition;
    float walked = 0f;

    void Start()
    {
        lastPosition = transform.position;
    }

    void Update()
    {
        walked = walked + Vector3.Distance(transform.position, lastPosition);
        lastPosition = transform.position;

        while (walked >= unitsPerGold)
        {
            Inventory.Add("Gold", 1);
            walked = walked - unitsPerGold;
        }
    }
}
