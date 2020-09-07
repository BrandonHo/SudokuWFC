using UnityEngine;

public class SudokuBoard
{
    public Vector2Int NumberOfAreasInBoard = new Vector2Int(3, 3);
    public Vector2Int NumberOfCubesPerArea = new Vector2Int(3, 3);
    public SudokuCubeData[,] SudokuBoardMatrix;

    public SudokuBoard(Vector2Int numberOfAreasInBoard, Vector2Int numberOfCubesPerArea)
    {
        NumberOfAreasInBoard = numberOfAreasInBoard;
        NumberOfCubesPerArea = numberOfCubesPerArea;

        SudokuBoardMatrix = new SudokuCubeData[NumberOfAreasInBoard.x * NumberOfCubesPerArea.x,
            NumberOfAreasInBoard.y * NumberOfCubesPerArea.y];

        // Initialise board with empty data (TODO: custom boards)
        for (int i = 0; i < SudokuBoardMatrix.GetLength(0); i++)
            for (int j = 0; j < SudokuBoardMatrix.GetLength(1); j++)
                SudokuBoardMatrix[i, j] = new SudokuCubeData(i, j);
    }

    public SudokuCubeData GetSudokuCubeData(Vector2Int cubeIndices)
    {
        return SudokuBoardMatrix[cubeIndices.x, cubeIndices.y];
    }

    public SudokuCubeData GetSudokuCubeData(int rowCubeIndex, int colCubeIndex)
    {
        return SudokuBoardMatrix[rowCubeIndex, colCubeIndex];
    }
}
