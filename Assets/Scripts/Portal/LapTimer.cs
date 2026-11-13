// LapTimer.cs 1.0.0
//
// A finish line with a clock. Reach it before Target Seconds and you win.
// Still driving when the clock passes Target Seconds and you lose.
//
// 1. Drop this file anywhere in Assets/ (not in Assets/Editor/).
// 2. Make the finish line: GameObject > 2D Object > Sprites > Square. Stretch
//    it thin across the track. Add Component > Box Collider 2D, check
//    Is Trigger. Add Component > LapTimer.
// 3. The car needs Tag: Player (or change Car Tag), a Rigidbody 2D and a
//    Collider 2D.
// 4. Start the car just past the line, facing away from it, so a whole lap
//    comes before the finish. Backing over the line doesn't count: the car
//    has to drive back over it, then do a whole lap.
//
// To find your time: set Target Seconds to 999 and drive one lap. The game
// stops at the line and the Console says your time. Stop Play, then set Target
// Seconds a little under that time.

using UnityEngine;

public class LapTimer : MonoBehaviour
{
    [Tooltip("The tag on the car.")]
    public string carTag = "Player";

    [Tooltip("Finish before this many seconds to win.")]
    public float targetSeconds = 30f;

    [Tooltip("The clock. Watch it count while the game is playing.")]
    public float seconds = 0f;

    bool over = false;
    float startSide = 0f;
    float enteredFrom = 0f;
    int backwardCrossings = 0;

    void Start()
    {
        GameObject car = GameObject.FindWithTag(carTag);
        if (car != null) startSide = Side(car.transform.position);
    }

    // Which side of the line a point is on: 1 or -1.
    float Side(Vector3 point)
    {
        // The line is thin, so its short side points along the track.
        Vector3 across = transform.lossyScale.x < transform.lossyScale.y ? transform.right : transform.up;
        return Mathf.Sign(Vector3.Dot(point - transform.position, across));
    }

    void Update()
    {
        if (over) return;

        seconds = seconds + Time.deltaTime;
        if (seconds > targetSeconds) End("You lose! Out of time.");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == carTag) enteredFrom = Side(other.transform.position);
    }

    // A crossing counts when the car comes out the other side of the line.
    void OnTriggerExit2D(Collider2D other)
    {
        if (over || other.tag != carTag) return;
        float leftTo = Side(other.transform.position);
        // Touched the line and came back, or started on it.
        if (leftTo == enteredFrom || enteredFrom == 0f) return;

        // The car starts past the line, so a real lap ends on the starting side.
        // Backing over the line has to be driven back over first.
        if (leftTo != startSide)
        {
            backwardCrossings = backwardCrossings + 1;
            return;
        }
        if (backwardCrossings > 0)
        {
            backwardCrossings = backwardCrossings - 1;
            return;
        }

        End("You win! Time: " + seconds.ToString("0.0") + " seconds");
    }

    void End(string message)
    {
        over = true;
        Debug.Log(message);
        Time.timeScale = 0f;
    }
}
