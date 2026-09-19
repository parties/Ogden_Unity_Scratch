using UnityEngine;

[ExecuteAlways]
public class FitBackgroundToCamera : MonoBehaviour
{
    void Start()
    {
        Fit();
        transform.SetParent(Camera.main.transform);
        transform.localPosition = new Vector3(0, 0, 10f);
    }

    void Fit()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Camera cam = Camera.main;
        float worldHeight = cam.orthographicSize * 2f;
        float worldWidth = worldHeight * cam.aspect;

        Vector2 spriteSize = sr.sprite.bounds.size;
        transform.localScale = new Vector3(
            worldWidth / spriteSize.x,
            worldHeight / spriteSize.y,
            1f);
    }
}