// Health.cs 1.0.0
//
// Hit points for a player, an enemy or a base (the thing enemies attack).
//
// 1. Drop this file anywhere in Assets/ (not in Assets/Editor/).
// 2. Select the player, an enemy or the base. Add Component > Health.
// 3. Set Max HP in the Inspector.
// 4. Something has to do the damage: ContactDamage, WaypointFollower or Tower.
//
// At 0 HP:
//   - the player (Tag: Player), or anything with Lose At Zero checked (the
//     base), prints "You lose!" in the Console and the game stops.
//   - anything else disappears. If it was tagged Enemy and it was the last
//     enemy, the Console prints "You win!" and the game stops.
//
// Watch HP in the Inspector while the game is playing.
//
// For a game over screen, make a scene for it, add it under File > Build Profiles
// > Scene List, and type its name into Game Over Scene.

using UnityEngine;
using UnityEngine.SceneManagement;

public class Health : MonoBehaviour
{
    [Tooltip("Hit points at the start.")]
    public int maxHP = 3;

    [Tooltip("Hit points right now. Watch it change while the game is playing.")]
    public int hp;

    [Tooltip("Seconds after a hit when it can't be hurt. Try 1 on the player, so a row of spikes takes 1 HP, not one per spike, and staying on spikes or against an enemy hurts again each second. Leave 0 on a base.")]
    public float safeTime = 0f;

    [Tooltip("Check this on the base in a tower defense game. At 0 HP you lose.")]
    public bool loseAtZero = false;

    [Tooltip("The scene to load when you lose. Leave empty to stop the game instead.")]
    public string gameOverScene = "";

    // EnemySpawner and RoomDoor turn this off when beating the last enemy
    // shouldn't end the game.
    public static bool winWhenEnemiesGone = true;

    // EnemySpawner counts itself here while it still has enemies to send.
    public static int spawnersWaiting = 0;

    float safeUntil = 0f;

    void Start()
    {
        hp = maxHP;
        // A resting body sleeps, and a sleeping body stops reporting touches,
        // so it would never get hurt again.
        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (safeTime > 0f && body != null) body.sleepMode = RigidbodySleepMode2D.NeverSleep;
    }

    public void TakeDamage(int amount)
    {
        if (hp <= 0) return;
        if (Time.time < safeUntil) return;
        safeUntil = Time.time + safeTime;

        hp = hp - amount;
        Debug.Log(name + " HP: " + hp);
        if (hp > 0) return;

        // tag == "..." instead of CompareTag, so a tag nobody added is no error.
        if (loseAtZero || tag == "Player")
        {
            Debug.Log("You lose!");
            if (gameOverScene != "")
            {
                SceneManager.LoadScene(gameOverScene);
                return;
            }
            Time.timeScale = 0f;
            return;
        }

        if (tag == "Enemy" && winWhenEnemiesGone && !EnemiesLeft())
        {
            Debug.Log("You win!");
            Time.timeScale = 0f;
        }
        Destroy(gameObject);
    }

    // Counts enemies still above 0 HP. Destroy waits until the end of the frame,
    // so a beaten enemy is still in the scene for a moment.
    public static bool EnemiesLeft()
    {
        foreach (GameObject enemy in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            Health health = enemy.GetComponent<Health>();
            if (health == null || health.hp > 0) return true;
        }
        return false;
    }
}
