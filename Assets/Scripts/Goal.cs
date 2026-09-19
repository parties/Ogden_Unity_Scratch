using UnityEngine;

public class Goal : MonoBehaviour
{
    public static bool won = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            won = true;
            Debug.Log("YOU WIN");
        }
    }
}
