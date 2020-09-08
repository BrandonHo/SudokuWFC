using UnityEngine;

/// <summary>
/// Struct that describes the necessary data for a single cube update. This is specifically
/// used for processing backtracking in the sudoku board.
/// </summary>
public struct BacktrackCubeData
{
    public int CubeNumber;
    public Vector2Int CubeIndices;
}
