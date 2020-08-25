using UnityEngine;

public class SudokuCubeData
{
    public int Number = 0;
    public Vector2Int CubeIndices;
    public bool[] AvailableNumbers = new bool[] { true, true, true, true, true, true, true, true, true };

    public SudokuCubeData(int rowIndex, int colIndex)
    {
        CubeIndices = new Vector2Int(rowIndex, colIndex);
    }

    public void SetCubeIndices(int rowCubeIndex, int colCubeIndex)
    {
        CubeIndices = new Vector2Int(rowCubeIndex, colCubeIndex);
    }
}