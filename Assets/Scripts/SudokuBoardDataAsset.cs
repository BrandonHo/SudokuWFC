using UnityEngine;

/// <summary>
/// Scriptable object that describes the data for a sudoku board.
/// </summary>
[CreateAssetMenu(fileName = "SudokuBoardData", menuName = "ScriptableObjects/SudokuBoardDataAsset", order = 1)]
public class SudokuBoardDataAsset : ScriptableObject
{
    [SerializeField] public SudokuBoardDataRow[] SudokuBoardDataRows;
    [SerializeField] public Vector2Int NumberOfAreasInBoard, NumberOfCubesPerArea;

    /// <summary>
    /// Initialisation method - called by selecting the appropriate button in the
    /// editor window for the sudoku board data asset.
    /// </summary>
    /// <param name="numberOfAreasInBoard">The number of areas in the sudoku board for the x and y axis.</param>
    /// <param name="numberOfCubesPerArea">The number of cubes per area in the sudoku board (x and y axis).</param>
    public void Initialise(Vector2Int numberOfAreasInBoard, Vector2Int numberOfCubesPerArea)
    {
        SudokuBoardDataRows = new SudokuBoardDataRow[numberOfAreasInBoard.x * numberOfCubesPerArea.x];
        for (int i = 0; i < SudokuBoardDataRows.Length; i++)
            SudokuBoardDataRows[i] = new SudokuBoardDataRow(i, numberOfAreasInBoard.y * numberOfCubesPerArea.y);

        NumberOfAreasInBoard = numberOfAreasInBoard;
        NumberOfCubesPerArea = numberOfCubesPerArea;
    }

    /// <summary>
    /// Method used to acquire the numbers in the sudoku board.
    /// </summary>
    /// <returns>A jagged integer array describing the numbers in the sudoku board.</returns>
    public int[,] GetSudokuBoardNumbers()
    {
        /*
            Keep in mind that the game world uses the bottom left as origin,
            while the editor window uses the top left as origin.

            Therefore the ith-row of the integer array actually refers to the
            (size-1-ith-row) of the sudoku board.
        */

        if (SudokuBoardDataRows == null)
            return null;

        int[,] sudokuBoardData = new int[NumberOfAreasInBoard.x * NumberOfCubesPerArea.x,
            NumberOfAreasInBoard.y * NumberOfCubesPerArea.y];

        for (int i = 0; i < sudokuBoardData.GetLength(0); i++)
            for (int j = 0; j < sudokuBoardData.GetLength(1); j++)
                sudokuBoardData[i, j] = SudokuBoardDataRows[sudokuBoardData.GetLength(0) - 1 - i].SudokuCubeDataRow[j].Number;

        return sudokuBoardData;
    }

    /// <summary>
    /// Method uses to update the numbers of the sudoku board.
    /// </summary>
    /// <param name="sudokuBoardData">A jagged integer array describing the numbers in the sudoku board.</param>
    public void SetSudokuBoardData(int[,] sudokuBoardData)
    {
        /*
            Keep in mind that the game world uses bottom left as origin,
            while the editor window uses the top left as origin.

            Therefore the ith-row of the integer array actually refers to the
            (size-1-ith-row) of the sudoku board.
        */

        for (int i = 0; i < sudokuBoardData.GetLength(0); i++)
            for (int j = 0; j < sudokuBoardData.GetLength(1); j++)
                SudokuBoardDataRows[sudokuBoardData.GetLength(0) - 1 - i].SudokuCubeDataRow[j].Number = sudokuBoardData[i, j];
    }

    public SudokuCubeData GetSudokuCubeData(int rowIndex, int colIndex)
    {
        return SudokuBoardDataRows[rowIndex].SudokuCubeDataRow[colIndex];
    }

    public SudokuCubeData GetSudokuCubeData(Vector2Int cubeIndices)
    {
        return GetSudokuCubeData(cubeIndices.x, cubeIndices.y);
    }
}
