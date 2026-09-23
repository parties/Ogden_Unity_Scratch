using UnityEngine;

public class Coin : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Coin collided with: " + other.tag);
        if (other.CompareTag("Player"))
        {
            ScoreCounter.coins = ScoreCounter.coins + 1;
            Destroy(gameObject);
        }
    }
}