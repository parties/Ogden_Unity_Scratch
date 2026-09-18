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
        Physics2D.queriesStartInColliders = false;  // don't detect the enemy's own collider

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerStart = player.transform.position;
        }
    }

    void Update()
    {
        transform.Translate(Vector3.right * direction * speed * Time.deltaTime);

        // Raycast, not OverlapPoint: the level's collider is Outlines — edges, no inside.
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            Vector2.right * direction,
            lookAhead
        );

        bool blocked = (
            hit.collider != null
            && !hit.collider.isTrigger
            && !hit.collider.CompareTag("Player")
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
            if (!other.CompareTag("Player")) return;

            // Is the player above us? Then we got stomped.
            if (other.transform.position.y > transform.position.y + 0.4f)
            {
                Destroy(gameObject);
            }
            else
            {
                other.transform.position = playerStart;
            }

            // reset the player
            other.transform.position = playerStart;
        }
    }
}
