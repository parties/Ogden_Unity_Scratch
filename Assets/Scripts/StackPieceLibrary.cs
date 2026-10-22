using UnityEngine;

[CreateAssetMenu(fileName = "StackPieceLibrary", menuName = "Game/Stack Piece Library")]
public class StackPieceLibrary : ScriptableObject
{
    public Sprite[] basePieces;
    public Sprite[] middlePieces;
    public Sprite[] topPieces;
}
