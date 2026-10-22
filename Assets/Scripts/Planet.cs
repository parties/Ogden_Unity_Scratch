using UnityEngine;

public class Planet : MonoBehaviour
{
    [SerializeField] private StackRandomizer stack;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Space");
            stack.Randomize();
        }
    }
}
