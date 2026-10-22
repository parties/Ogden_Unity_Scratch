using UnityEngine;

public class StackRandomizer : MonoBehaviour
{
    [SerializeField] private StackPieceLibrary library;

    [SerializeField] private SpriteRenderer baseRenderer;
    [SerializeField] private SpriteRenderer middleRenderer;
    [SerializeField] private SpriteRenderer topRenderer;

    [SerializeField] private bool randomizeOnStart = true;

    void Start()
    {
        if (randomizeOnStart) Randomize();
    }

    public void Randomize()
    {
        Debug.Log("Randomizing...");
        baseRenderer.sprite = PickRandom(library.basePieces);
        middleRenderer.sprite = PickRandom(library.middlePieces);
        topRenderer.sprite = PickRandom(library.topPieces);
    }

    private Sprite PickRandom(Sprite[] pool)
    {
        if (pool == null || pool.Length == 0) return null;
        return pool[Random.Range(0, pool.Length)];   // int overload: max is exclusive
    }

    // Lets you test from the Inspector's ⋮ menu without entering Play mode
    [ContextMenu("Randomize")]
    private void RandomizeFromMenu() => Randomize();
}
