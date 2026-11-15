// EnemySpawner.cs 1.0.0
//
// Makes enemies. It can spawn when the game starts, when you press a key, or
// when the player walks into a room.
//
// 1. Download Health.cs too. Put both anywhere in Assets/ (not in Assets/Editor/).
// 2. GameObject > Create Empty, name it Spawner. Add Component > EnemySpawner.
// 3. Drag your enemies into Enemies. A prefab works. So does an enemy from the
//    Hierarchy: it gets hidden and copied, and the copies keep its Path and Base
//    Health. Use that way for tower defense. With more than one, each spawn picks
//    one at random.
// 4. Spawn points are optional. Make empty objects where enemies should appear
//    and drag them into Spawn Points. Enemies use them in order, then start over.
//
// When does it spawn?
//   Game start: check Spawn At Start.
//   A key: pick it in Spawn Key. Only When All Gone waits for a clear screen.
//   Entering a room: uncheck Spawn At Start. Add a Box Collider 2D to the Spawner,
//   check Is Trigger, and stretch it across the doorway.
//
// With a key or a room, killing the last enemy doesn't win the game, since more
// can still come.

using UnityEngine;
using UnityEngine.InputSystem;

public class EnemySpawner : MonoBehaviour
{
    [Tooltip("What to spawn. With more than one, each spawn picks at random.")]
    public GameObject[] enemies;

    [Tooltip("Where to spawn. Leave empty to spawn right here.")]
    public Transform[] spawnPoints;

    [Tooltip("How many enemies each time.")]
    public int howMany = 3;

    [Tooltip("Seconds between each enemy. 0 means all at once.")]
    public float secondsBetween = 0.5f;

    [Tooltip("Spawn when the game starts.")]
    public bool spawnAtStart = true;

    [Tooltip("The key that spawns. None means no key.")]
    public Key spawnKey = Key.None;

    [Tooltip("The key only works once every enemy is gone.")]
    public bool onlyWhenAllGone = true;

    int left = 0;
    int nextPoint = 0;
    float timer = 0f;
    bool entered = false;
    bool pending = false;

    void Start()
    {
        foreach (GameObject enemy in enemies)
        {
            // An enemy from the Hierarchy is only the pattern for the copies.
            if (enemy.scene.IsValid()) enemy.SetActive(false);
        }

        // A room spawner nobody has walked into yet still has enemies to send.
        if (!spawnAtStart && spawnKey == Key.None)
        {
            SetPending(true);
            if (GetComponent<Collider2D>() == null) Debug.LogWarning(name + " waits for the player but has no Box Collider 2D, so it never spawns.");
        }
        if (spawnAtStart) StartWave();
        Health.winWhenEnemiesGone = false;
    }

    void StartWave()
    {
        if (enemies.Length == 0)
        {
            Debug.LogWarning(name + " has nothing in Enemies to spawn.");
            return;
        }

        left = howMany;
        timer = secondsBetween;
        SetPending(true);
    }

    void Update()
    {
        Keyboard keys = Keyboard.current;
        if (spawnKey != Key.None && keys != null && keys[spawnKey].wasPressedThisFrame)
        {
            if (left == 0 && (!onlyWhenAllGone || !Health.EnemiesLeft())) StartWave();
        }

        timer = timer + Time.deltaTime;
        while (left > 0 && timer >= secondsBetween)
        {
            SpawnOne();
            left = left - 1;
            timer = timer - secondsBetween;

            // A game-start wave with nothing more coming: the last enemy wins the game.
            if (left == 0 && spawnAtStart && spawnKey == Key.None) Health.winWhenEnemiesGone = true;
            if (left == 0) SetPending(false);
        }
    }

    void SpawnOne()
    {
        Vector3 where = transform.position;
        if (spawnPoints.Length > 0)
        {
            where = spawnPoints[nextPoint % spawnPoints.Length].position;
            nextPoint = nextPoint + 1;
        }

        GameObject pattern = enemies[Random.Range(0, enemies.Length)];
        GameObject enemy = Instantiate(pattern, where, Quaternion.identity);
        enemy.SetActive(true);
    }

    // Leaving this scene: the next one's last enemy wins unless it has its own
    // door or spawner.
    void OnDestroy()
    {
        Health.winWhenEnemiesGone = true;
        SetPending(false);
    }

    // Health.spawnersWaiting counts spawners with enemies still to send.
    // RoomDoor stays locked while it's above 0.
    void SetPending(bool on)
    {
        if (on == pending) return;
        pending = on;
        Health.spawnersWaiting = Health.spawnersWaiting + (on ? 1 : -1);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (entered || other.tag != "Player") return;

        entered = true;
        StartWave();
    }
}
