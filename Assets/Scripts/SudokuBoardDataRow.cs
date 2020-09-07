using UnityEngine;

/// <summary>
/// Class that describes a row of cube data for a sudoku board. This class is 
/// created as a work-around for storing sudoku board data, as Unity does not
/// serialise jagged arrays.
/// </summary>
[System.Serializable]
public class SudokuBoardDataRow
{
    [SerializeField] public int RowIndex;
    [SerializeField] public SudokuCubeData[] SudokuCubeDataRow;

    public SudokuBoardDataRow(int rowIndex, int size)
    {
        RowIndex = rowIndex;
        SudokuCubeDataRow = new SudokuCubeData[size];
        for (int colIndex = 0; colIndex < SudokuCubeDataRow.Length; colIndex++)
            SudokuCubeDataRow[colIndex] = new SudokuCubeData(rowIndex, colIndex);
    }
}
