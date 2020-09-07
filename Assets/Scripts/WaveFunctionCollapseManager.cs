using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveFunctionCollapseManager : MonoBehaviour
{
    public SudokuBoardDataAsset SudokuBoardDataAsset1;
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
        // Only do initialisation if asset is assigned
        if (SudokuBoardDataAsset1 != null)
        {
            Random.InitState(RandomSeed);
            BacktrackStack = new Stack<BacktrackBoardUpdateData>();
            SudokuBoardData = new SudokuBoard(NumberOfAreasInBoard, NumberOfCubesPerArea);

            SudokuBoardController.InitialiseBoard(SudokuBoardDataAsset1, OffsetBetweenAreas);
            SudokuBoardController.SetupAvailableCountMapsForCubes();
        }
        else
            Debug.LogError("Sudoku board data asset required for board controller.");
    }

    public void StartWFCSolver()
    {
        if (SudokuBoardDataAsset1)
        {
            // Initialise and start coroutine to periodically perform WFC
            CoroutineWFC = PerformWFC();
            CoroutineDelay = new WaitForSeconds(UpdateDelayInSeconds);
            StartCoroutine(CoroutineWFC);
        }
        else
            Debug.LogError("Sudoku board data asset required for board controller.");
    }

    IEnumerator PerformWFC()
    {
        while (true)
        {
            // Continue to process the WFC algorithm until board is complete
            if (!SudokuBoardController.IsBoardComplete())
                PerformNextWFCStep();

            yield return CoroutineDelay;
        }
    }

    public void PerformNextWFCStep()
    {
        if (SudokuBoardController.IsBoardValid())
        {
            // Select next cube with lowest entropy
            CubeController selectedCube = SudokuBoardController.SelectLowestEntropyCube();
            int nextAvailableNumber = SudokuBoardDataAsset1.GetSudokuCubeData(selectedCube.CubeIndices).GetRandomAvailableNumber();
            // SudokuBoardData.GetSudokuCubeData(selectedCube.CubeIndices).GetRandomAvailableNumber();

            // Add cube data to backtrack stack + process cube selection
            BacktrackStack.Push(new BacktrackBoardUpdateData(nextAvailableNumber, selectedCube.CubeIndices));
            SudokuBoardController.SelectNumberForCube(nextAvailableNumber, selectedCube.CubeIndices);
        }
        else
        {
            // Get last cube selection and revert changes
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

    /// <summary>
    /// Method callback from manual interaction with the "Process" button in the GUI.
    /// Used for debugging purposes.
    /// </summary>
    public void PerformNextWFCStepCallback()
    {
        // Only perform the next WFC step if the asset is actually defined
        if (SudokuBoardDataAsset1)
            PerformNextWFCStep();
        else
            Debug.LogError("Sudoku board data asset required for board controller.");
    }

    void OnDrawGizmos()
    {
        if (IsDebugModeOn)
        {
            Gizmos.color = Color.red;

            // Draw debug gizmo that indicates the outer boundary of the sudoku board
            Vector3 totalWidth = new Vector3(NumberOfAreasInBoard.x * NumberOfCubesPerArea.x, 0.1f,
                NumberOfAreasInBoard.y * NumberOfCubesPerArea.y);
            totalWidth += new Vector3((NumberOfAreasInBoard.x - 1) * OffsetBetweenAreas, 0, (NumberOfAreasInBoard.x - 1) * OffsetBetweenAreas);
            Gizmos.DrawWireCube(transform.position, totalWidth);
        }
    }
}
