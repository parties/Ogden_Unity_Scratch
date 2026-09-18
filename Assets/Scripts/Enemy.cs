using UnityEngine;

// Moves back and forth between two points. Touching the player sends the
// player back to where the player started.
public class Enemy : MonoBehaviour
{
    public float speed = 2f;       // how fast the enemy walks
    public float distance = 3f;    // how far it walks before turning around

    public float lookAhead = 0.6f;   // just past the edge of your sprite — tune this


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

        // Peek one step ahead of the enemy's middle. Something solid there? Turn now.
        Vector2 ahead = new Vector2(transform.position.x + direction * lookAhead,
                                    transform.position.y);
        Collider2D wall = Physics2D.OverlapPoint(ahead);

        bool blocked = (
            wall != null
            && !wall.isTrigger                // ignore coins, flags, our own trigger
            && wall.gameObject != gameObject  // ignore ourselves
            && !wall.CompareTag("Player")
        );

        if (blocked || Mathf.Abs(transform.position.x - startPosition.x) >= distance)
        {
            direction = -direction;
            startPosition = transform.position;   // start measuring the walk from here
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
