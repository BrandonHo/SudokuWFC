using UnityEngine;
using System.Collections.Generic;

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

    /// <summary>
    /// This helper method is used to checked whether the board is complete and no longer needs
    /// to be processed.
    /// </summary>
    /// <returns>True if board complete. False if board not complete.</returns>
    public bool IsBoardComplete()
    {
        int counter = 0;

        for (int i = 0; i < SudokuBoardMatrix.GetLength(0); i++)
            for (int j = 0; j < SudokuBoardMatrix.GetLength(1); j++)
                if (SudokuBoardMatrix[i, j].Number != 0)
                    counter++;

        return counter == (NumberOfAreasInBoard.x * NumberOfCubesPerArea.x * NumberOfAreasInBoard.y * NumberOfCubesPerArea.y);
    }

    public SudokuCubeData[,] CopySudokuBoardValueMatrix()
    {
        SudokuCubeData[,] matrixCopy = new SudokuCubeData[SudokuBoardMatrix.GetLength(0), SudokuBoardMatrix.GetLength(1)];

        for (int i = 0; i < SudokuBoardMatrix.GetLength(0); i++)
            for (int j = 0; j < SudokuBoardMatrix.GetLength(1); j++)
                matrixCopy[i, j] = SudokuBoardMatrix[i, j];

        return matrixCopy;
    }

    public void ResetSudokuBoardValuesUsingMatrix(SudokuCubeData[,] resetValueMatrix)
    {
        for (int i = 0; i < SudokuBoardMatrix.GetLength(0); i++)
            for (int j = 0; j < SudokuBoardMatrix.GetLength(1); j++)
                SudokuBoardMatrix[i, j] = resetValueMatrix[i, j];
    }
}
