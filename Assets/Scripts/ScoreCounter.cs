using UnityEngine;

public class ScoreCounter : MonoBehaviour
{
    public static int coins = 0;

    void Start()
    {
        coins = 0;
    }

    void OnGUI()
    {
        GUI.Label(new Rect(20f, 20f, 200f, 40f), "Coins: " + coins);
    }
}