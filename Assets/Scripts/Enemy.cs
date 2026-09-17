using UnityEngine;

// Moves back and forth between two points. Touching the player sends the
// player back to where the player started.
public class Enemy : MonoBehaviour
{
    public float speed = 2f;       // how fast the enemy walks
    public float distance = 3f;    // how far it walks before turning around

    private Vector3 startPosition;
    private Vector3 playerStart;
    private int direction = 1;     // 1 is right, -1 is left

    void Start()
    {
        startPosition = transform.position;

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerStart = player.transform.position;
        }
    }

    void Update()
    {
        transform.Translate(Vector3.right * direction * speed * Time.deltaTime);

        if (Mathf.Abs(transform.position.x - startPosition.x) >= distance)
        {
            direction = -direction;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // check whether the player is above the enemy and falling.
            // other.transform.position.y > this.
            // If it is, destroy the enemy (see below) instead of moving the player.
            // Two things to look up: Destroy(gameObject), and how to


            // reset the player
            other.transform.position = playerStart;
        }
    }
}
