using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#pragma warning disable 0649  
public class WaveFunctionCollapseManager : MonoBehaviour
{
    public SudokuBoardDataAsset SudokuBoardData;
    public BoardController SudokuBoardController;
    private Stack<BacktrackBoardUpdateData> BacktrackStack;

    [SerializeField] private int RandomSeed;
    [SerializeField] private float UpdateDelayInSeconds;
    [SerializeField] private float OffsetBetweenAreas;
    [SerializeField] private bool IsDebugModeOn;

    private IEnumerator CoroutineWFC;
    private WaitForSeconds CoroutineDelay;

    void Start()
    {
        // Only do initialisation if asset is assigned
        if (SudokuBoardData != null)
        {
            Random.InitState(RandomSeed);
            BacktrackStack = new Stack<BacktrackBoardUpdateData>();
            SudokuBoardData.ResetBoardAsset();
            SudokuBoardController.CreateSudokuBoard(SudokuBoardData, OffsetBetweenAreas);
            SudokuBoardController.SetupAvailableCountMapsForCubes();
        }
        else
            Debug.LogError("Sudoku board data asset required for board controller.");
    }

    public void StartWFCSolver()
    {
        if (SudokuBoardData)
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

    /// <summary>
    /// Method callback from manual interaction with the "Process" button in the GUI.
    /// Used for debugging purposes.
    /// </summary>
    public void PerformNextWFCStepCallback()
    {
        // Only perform the next WFC step if the asset is actually defined
        if (SudokuBoardData)
            PerformNextWFCStep();
        else
            Debug.LogError("Sudoku board data asset required for board controller.");
    }

    /// <summary>
    /// Primary method for performing the WFC algorithm.
    /// </summary>
    public void PerformNextWFCStep()
    {
        if (SudokuBoardController.IsBoardValid())
        {
            // Select next cube with lowest entropy
            CubeController selectedCube = SudokuBoardController.SelectLowestEntropyCube();
            int nextAvailableNumber = SudokuBoardData.GetSudokuCubeData(selectedCube.CubeIndices).GetRandomAvailableNumber();
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

    void OnDrawGizmos()
    {
        if (IsDebugModeOn)
        {
            if (SudokuBoardData)
            {
                Gizmos.color = Color.red;

                // Draw debug gizmo that indicates the outer boundary of the sudoku board
                Vector3 totalWidth = new Vector3(SudokuBoardData.NumberOfAreasInBoard.x * SudokuBoardData.NumberOfCubesPerArea.x, 0.1f,
                    SudokuBoardData.NumberOfAreasInBoard.y * SudokuBoardData.NumberOfCubesPerArea.y);
                totalWidth += new Vector3((SudokuBoardData.NumberOfAreasInBoard.x - 1) * OffsetBetweenAreas, 0,
                    (SudokuBoardData.NumberOfAreasInBoard.x - 1) * OffsetBetweenAreas);
                Gizmos.DrawWireCube(transform.position, totalWidth);
            }
        }
    }
}
