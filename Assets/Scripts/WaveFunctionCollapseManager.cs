using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveFunctionCollapseManager : MonoBehaviour
{
    public BoardController SudokuBoardController;
    public SudokuBoard SudokuBoardData;

    public Stack<BacktrackStateData> BacktrackStack;

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

        BacktrackStack = new Stack<BacktrackStateData>();

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

    private void ProcessGuess()
    {
        if (SudokuBoardController.IsBoardValid())
        {
            // Select next cube with lowest entropy
            Vector2Int selectedCubeIndices = SudokuBoardController.SelectLowestEntropyCube();
            int nextAvailableNumber = SudokuBoardData.GetSudokuCubeData(selectedCubeIndices).GetRandomAvailableNumber();



            SudokuBoardController.SelectNumberForCube(nextAvailableNumber, selectedCubeIndices);
        }
        else
        {
            SudokuBoardController.RevertLastBoardUpdate();
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
