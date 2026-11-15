// RoomDoor.cs 1.0.0
//
// A door that opens when every enemy is gone. Walk into it to go to another room.
//
// 1. Download Health.cs too. Put both anywhere in Assets/ (not in Assets/Editor/).
// 2. Each room is its own scene. Add them all under File > Build Profiles >
//    Scene List, or Unity can't load them.
// 3. Select the door. Add Component > Box Collider 2D, check Is Trigger, then
//    Add Component > RoomDoor.
// 4. Type scene names into Next Rooms, exactly as they're spelled. With more than
//    one, the door picks at random.
// 5. The player needs Tag: Player and a Rigidbody 2D. Enemies need Tag: Enemy and
//    Health.
//
// The door is red while it's locked and green when it's open.
//
// A game over screen: make a scene for it, add it to the Scene List, and type its
// name into Game Over Scene on the player's Health. Each new scene starts with
// the Max HP you set there, so the player's HP doesn't carry over between rooms.
// The Inventory starts over too.

using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomDoor : MonoBehaviour
{
    [Tooltip("The scenes this door can lead to, spelled exactly.")]
    public string[] nextRooms;

    SpriteRenderer sprite;

    void Start()
    {
        sprite = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // Beating the last enemy opens the door instead of winning the game.
        Health.winWhenEnemiesGone = false;

        if (sprite != null) sprite.color = Locked() ? Color.red : Color.green;
    }

    // Locked while enemies are left, or while a spawner still has more to send.
    bool Locked()
    {
        return Health.EnemiesLeft() || Health.spawnersWaiting > 0;
    }

    // Leaving this scene: the next one's last enemy wins unless it has its own
    // door or spawner.
    void OnDestroy()
    {
        Health.winWhenEnemiesGone = true;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.tag != "Player" || Locked()) return;

        if (nextRooms.Length == 0)
        {
            Debug.LogWarning(name + " has no scenes in Next Rooms.");
            return;
        }

        string room = nextRooms[Random.Range(0, nextRooms.Length)];
        if (!Application.CanStreamedLevelBeLoaded(room))
        {
            Debug.LogError("Can't load " + room + ". Check the spelling and add it under File > Build Profiles > Scene List.");
            return;
        }

        SceneManager.LoadScene(room);
    }
}
