using UnityEngine;
using System.Collections.Generic;

public class BacktrackBoardUpdateData
{
    public BacktrackCubeData SelectedCubeData;
    public Dictionary<int, List<BacktrackCubeData>> InvalidCubeData;

    public BacktrackBoardUpdateData(int cubeNumber, Vector2Int cubeIndices)
    {
        SelectedCubeData = new BacktrackCubeData()
        {
            CubeNumber = cubeNumber,
            CubeIndices = cubeIndices
        };

        InvalidCubeData = new Dictionary<int, List<BacktrackCubeData>>();
    }

    public void AddInvalidCubeData(Vector2Int cubeIndices, int cubeNumber)
    {
        if (!InvalidCubeData.ContainsKey(cubeNumber))
            InvalidCubeData.Add(cubeNumber, new List<BacktrackCubeData>());

        InvalidCubeData[cubeNumber].Add(new BacktrackCubeData()
        {
            CubeNumber = cubeNumber,
            CubeIndices = cubeIndices
        });
    }
}
