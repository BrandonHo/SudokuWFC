using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveFunctionCollapseManager : MonoBehaviour
{
    public BoardController SudokuBoardController;
    public SudokuBoard SudokuBoardData;
    public Stack<BacktrackStateData> BacktrackNumberUpdates;
    public int RandomSeed;
    public float UpdateDelayInSeconds = 0.5f;

    public Vector2Int NumberOfAreasInBoard;
    public Vector2Int NumberOfCubesPerArea;
    public float OffsetBetweenAreas = 0.1f;
    public bool IsDebugModeOn;

    private IEnumerator CoroutineWFC;
    private WaitForSeconds CoroutineDelay;

    void Start()
    {
        Random.InitState(RandomSeed);
        BacktrackNumberUpdates = new Stack<BacktrackStateData>();
        SudokuBoardData = new SudokuBoard(NumberOfAreasInBoard, NumberOfCubesPerArea);
        SudokuBoardController.InstantiateBoard(NumberOfAreasInBoard, NumberOfCubesPerArea, OffsetBetweenAreas, SudokuBoardData);
        SudokuBoardController.AddListenerToCubeUpdateEvent(CubeUpdateCallback);
    }

    public void CubeUpdateCallback(int number, Vector2Int cubeIndices)
    {
        SudokuBoardData.GetSudokuCubeData(cubeIndices).Number = number;
    }

    public void PerformWFCCallback()
    {
        CoroutineWFC = PerformWFC();
        CoroutineDelay = new WaitForSeconds(UpdateDelayInSeconds);
        StartCoroutine(CoroutineWFC);
    }

    IEnumerator PerformWFC()
    {
        while (true)
        {
            // Only process if board incomplete
            if (!SudokuBoardController.IsBoardComplete())
            {
                ProcessGuess();
            }

            yield return CoroutineDelay;
        }
    }

    private void ProcessGuess()
    {
        Vector2Int cubeIndices;
        bool boardResult = SudokuBoardController.IsBoardValid(out cubeIndices);
        if (boardResult)
        {
            // Select next cube with lowest entropy
            int availableNumberCount = 0;
            Vector2Int selectedCubeIndices = SudokuBoardController.SelectLowestEntropyCube(out availableNumberCount);

            // Should never occur - but cater for case where no eligble
            if ((selectedCubeIndices.x != -1) && (selectedCubeIndices.y != -1))
            {
                int nextAvailableNumber = SudokuBoardData.GetSudokuCubeData(selectedCubeIndices).GetRandomAvailableNumber();

                // If only one number left - solve, propagate
                if (availableNumberCount == 1)
                {
                    SudokuBoardController.SelectNumberForCube(nextAvailableNumber, selectedCubeIndices);
                }
                else
                {
                    // Not certain if number is correct - need to save state
                    BacktrackStateData newGuessStack = new BacktrackStateData(nextAvailableNumber, selectedCubeIndices, SudokuBoardData.CopySudokuBoardValueMatrix());
                    BacktrackNumberUpdates.Push(newGuessStack);

                    // ... Then try selecting the number
                    SudokuBoardController.SelectNumberForCube(nextAvailableNumber, selectedCubeIndices);
                }
            }
        }
        else
        {
            if (BacktrackNumberUpdates.Count != 0)
            {
                BacktrackStateData recentGuess = BacktrackNumberUpdates.Pop();
                SudokuBoardController.UpdateBoardWithNewData(recentGuess.SudokuDataCopy);
                SudokuBoardController.DisableNumberState(recentGuess.GuessCubeIndices, recentGuess.GuessNumber, false, false);
                SudokuBoardController.ReverseAvailableCountMapUpdate(cubeIndices, true);
            }
        }
    }

    void OnDrawGizmos()
    {
        if (IsDebugModeOn)
        {
            Gizmos.color = Color.red;

            // Draw debug gizmo that indicates the outer board
            Vector3 totalWidth = new Vector3(NumberOfAreasInBoard.x * NumberOfCubesPerArea.x, 0.1f,
                NumberOfAreasInBoard.y * NumberOfCubesPerArea.y);
            totalWidth += new Vector3((NumberOfAreasInBoard.x - 1) * OffsetBetweenAreas, 0, (NumberOfAreasInBoard.x - 1) * OffsetBetweenAreas);
            Gizmos.DrawWireCube(transform.position, totalWidth);
        }
    }
}
