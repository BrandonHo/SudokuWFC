using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Class that describes the necessary data required for recording and processing backtracking
/// states in the sudoku board.
/// </summary>
public class BacktrackBoardUpdateData
{
    public BacktrackCubeData SelectedCubeData;
    public List<BacktrackCubeData> InvalidCubeData;

    public BacktrackBoardUpdateData(int cubeNumber, Vector2Int cubeIndices)
    {
        SelectedCubeData = new BacktrackCubeData()
        {
            CubeNumber = cubeNumber,
            CubeIndices = cubeIndices
        };

        InvalidCubeData = new List<BacktrackCubeData>();
    }

    public void AddInvalidCubeData(Vector2Int cubeIndices, int cubeNumber)
    {
        InvalidCubeData.Add(new BacktrackCubeData()
        {
            CubeNumber = cubeNumber,
            CubeIndices = cubeIndices
        });
    }
}
