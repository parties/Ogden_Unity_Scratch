using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public int score = 0;
    public float smoothTime = 0.15f;

    private Vector3 velocity = Vector3.zero;

    void LateUpdate()
    {
        // exit if there is nothing to follow
        if (target == null) return;

        Vector3 goal = new Vector3(
            target.position.x,
            target.position.y,
            transform.position.z
        );

        transform.position = Vector3.SmoothDamp(
            transform.position, goal, ref velocity, smoothTime
        );
    }
}
