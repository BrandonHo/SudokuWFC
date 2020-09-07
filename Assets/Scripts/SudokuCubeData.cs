using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SudokuCubeData
{
    [SerializeField] public int Number = 0;
    [SerializeField] public Vector2Int CubeIndices;
    [SerializeField] public bool[] AvailableNumbers = new bool[] { true, true, true, true, true, true, true, true, true };

    public SudokuCubeData(int rowIndex, int colIndex)
    {
        CubeIndices = new Vector2Int(rowIndex, colIndex);
    }

    public int GetRandomAvailableNumber()
    {
        List<int> tempList = new List<int>();
        for (int i = 0; i < AvailableNumbers.Length; i++)
            if (AvailableNumbers[i])
                tempList.Add(i + 1);

        int test = tempList.Count;

        return tempList[Random.Range(0, tempList.Count)];
    }

    public SudokuCubeData CopyData()
    {
        SudokuCubeData newCopy = new SudokuCubeData(CubeIndices.x, CubeIndices.y);
        newCopy.Number = Number;

        for (int i = 0; i < AvailableNumbers.Length; i++)
            newCopy.AvailableNumbers[i] = AvailableNumbers[i];

        return newCopy;
    }
}