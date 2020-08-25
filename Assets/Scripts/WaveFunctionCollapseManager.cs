using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveFunctionCollapseManager : MonoBehaviour
{
    public BoardController SudokuBoardController;
    public SudokuBoard SudokuBoardData;
    public Stack<NumberCubeIndicesPair> BacktrackNumberUpdates;
    public int RandomSeed;

    public Vector2Int NumberOfAreasInBoard;
    public Vector2Int NumberOfCubesPerArea;
    public float OffsetBetweenAreas = 0.1f;
    public bool IsDebugModeOn;

    void Start()
    {
        Random.InitState(RandomSeed);
        SudokuBoardData = new SudokuBoard(NumberOfAreasInBoard, NumberOfCubesPerArea);
        SudokuBoardController.InstantiateBoard(NumberOfAreasInBoard, NumberOfCubesPerArea, OffsetBetweenAreas, SudokuBoardData);
    }

    void Update()
    {

    }

    public void PerformWFC()
    {

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
