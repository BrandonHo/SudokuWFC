using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveFunctionCollapseManager : MonoBehaviour
{
    public BoardController SudokuBoardController;
    public SudokuBoard SudokuBoardData;

    public Stack<BacktrackBoardUpdateData> BacktrackStack;
    public int RandomSeed;
    public float UpdateDelayInSeconds;

    public Vector2Int NumberOfAreasInBoard;
    public Vector2Int NumberOfCubesPerArea;
    public float OffsetBetweenAreas;
    public bool IsDebugModeOn;

    private IEnumerator CoroutineWFC;
    private WaitForSeconds CoroutineDelay;

    void Start()
    {
        Random.InitState(RandomSeed);

        BacktrackStack = new Stack<BacktrackBoardUpdateData>();

        SudokuBoardData = new SudokuBoard(NumberOfAreasInBoard, NumberOfCubesPerArea);
        SudokuBoardController.InstantiateBoard(NumberOfAreasInBoard, NumberOfCubesPerArea, OffsetBetweenAreas, SudokuBoardData);
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

    public void ProcessGuess()
    {
        if (SudokuBoardController.IsBoardValid())
        {
            // Select next cube with lowest entropy
            Vector2Int selectedCubeIndices = SudokuBoardController.SelectLowestEntropyCube();
            int nextAvailableNumber = SudokuBoardData.GetSudokuCubeData(selectedCubeIndices).GetRandomAvailableNumber();

            BacktrackStack.Push(new BacktrackBoardUpdateData(nextAvailableNumber, selectedCubeIndices));

            SudokuBoardController.SelectNumberForCube(nextAvailableNumber, selectedCubeIndices);
        }
        else
        {
            /*
                BUG:
                - Multiple backtracking can lead to an endless loop
                - Pretty convinced that it is due to loss of backtracking information and re-enablement
            */

            // Get last board update and revert changes
            if (BacktrackStack.Count != 0)
            {
                BacktrackBoardUpdateData lastBoardUpdate = BacktrackStack.Pop();
                SudokuBoardController.RevertCubeUpdate(lastBoardUpdate.SelectedCubeData.CubeIndices,
                    lastBoardUpdate.SelectedCubeData.CubeNumber, lastBoardUpdate.InvalidCubeData);

                // If there are more backtracking info in stack, notify previous stack about the invalid cube result
                if (BacktrackStack.Count != 0)
                {
                    BacktrackStack.Peek().AddInvalidCubeData(lastBoardUpdate.SelectedCubeData.CubeIndices,
                        lastBoardUpdate.SelectedCubeData.CubeNumber);
                }
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
