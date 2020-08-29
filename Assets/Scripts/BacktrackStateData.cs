using UnityEngine;

public class BacktrackStateData
{
    public int GuessNumber;
    public Vector2Int GuessCubeIndices;
    public SudokuCubeData[,] SudokuDataCopy;

    public BacktrackStateData(int guessNumber, Vector2Int guessCubeIndices, SudokuCubeData[,] sudokuDataCopy)
    {
        GuessNumber = guessNumber;
        GuessCubeIndices = guessCubeIndices;
        SudokuDataCopy = sudokuDataCopy;
    }
}
