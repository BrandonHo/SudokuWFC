using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class BoardController : MonoBehaviour
{
    public GameObject CubePrefab;
    private CubeController[,] CubeControllerMatrix;

    private Dictionary<int, List<CubeController>> NumberCountToCubeListMap;
    private List<CubeController> SelectedCubeList;

    private Stack<BacktrackStateData> BoardUpdateStack;

    private Vector2Int NumberOfAreasInBoard;
    private Vector2Int NumberOfCubesPerArea;

    public void InstantiateBoard(Vector2Int numberOfAreasInBoard, Vector2Int numberOfCubesPerArea, float offsetBetweenAreas, SudokuBoard boardData)
    {
        NumberCountToCubeListMap = new Dictionary<int, List<CubeController>>();
        SelectedCubeList = new List<CubeController>();
        for (int x = 0; x < 10; x++)
            NumberCountToCubeListMap.Add(x, new List<CubeController>());
        BoardUpdateStack = new Stack<BacktrackStateData>();

        NumberOfAreasInBoard = numberOfAreasInBoard;
        NumberOfCubesPerArea = numberOfCubesPerArea;

        // Calculate board dimensions + initialise matrix for storing cubes of the sudoku board
        Vector3 boardDimensions = new Vector3(numberOfAreasInBoard.x * numberOfCubesPerArea.x,
            0, numberOfAreasInBoard.y * numberOfCubesPerArea.y);
        CubeControllerMatrix = new CubeController[(int)boardDimensions.x, (int)boardDimensions.z];

        // Initial position of cube + offset vector for correcting the position of instantiated cubes
        Vector3 cubePosition = Vector3.zero + transform.position;
        Vector3 offsetCorrection = new Vector3(boardDimensions.x / 2 - (numberOfAreasInBoard.x - 1) * (numberOfAreasInBoard.y - 1) * offsetBetweenAreas,
            0f, boardDimensions.z / 2 - (numberOfAreasInBoard.x - 1) * (numberOfAreasInBoard.y - 1) * offsetBetweenAreas);

        for (int i = 0; i < CubeControllerMatrix.GetLength(0); i++)
        {
            // Add row offset between the areas
            if ((i != 0) && (i % numberOfCubesPerArea.x == 0))
                cubePosition += new Vector3(0f, 0f, offsetBetweenAreas);

            for (int j = 0; j < CubeControllerMatrix.GetLength(1); j++)
            {
                // Add column offset between the areas
                if ((j != 0) && (j % numberOfCubesPerArea.y == 0))
                    cubePosition += new Vector3(offsetBetweenAreas, 0f, 0f);

                // Instantiate cube + set parenting for the cubes
                GameObject newCube = Instantiate(CubePrefab, cubePosition - offsetCorrection, Quaternion.identity);
                newCube.transform.SetParent(transform);

                // Most importantly, reference the cube controller component in the matrix
                CubeControllerMatrix[i, j] = newCube.GetComponent<CubeController>();

                // Update cube indices + add callback for cube update events
                CubeControllerMatrix[i, j].SetCubeData(boardData.GetSudokuCubeData(i, j));
                AddCubeToAvailableCountMap(i, j);
                CubeControllerMatrix[i, j].AddOnCubeEventListener(OnCubeUpdateCallback);
                CubeControllerMatrix[i, j].AddOnAvailableNumbersUpdateEventListener(UpdateAvailableNumberCountMapSelect, true, false);
                CubeControllerMatrix[i, j].AddOnAvailableNumbersUpdateEventListener(UpdateAvailableNumberCountMapDeselect, false, true);
                CubeControllerMatrix[i, j].AddOnAvailableNumbersUpdateEventListener(UpdateAvailableNumberCountMapPropagate, false, false);

                // Adjust column position for next cube
                cubePosition += new Vector3(1f, 0f, 0f);
            }

            // Reset column position + adjust row position for next row of cubes
            cubePosition = new Vector3(0f, 0f, cubePosition.z + 1f);
        }
    }

    /// <summary>
    /// Primary method for selecting numbers on the board using the WFC algorithm.
    /// </summary>
    /// <param name="number">Number selected for a cube in the board.</param>
    /// <param name="cubeIndices">Indices of a cube in the board.</param>
    public void SelectNumberForCube(int number, Vector2Int cubeIndices)
    {
        CubeControllerMatrix[cubeIndices.x, cubeIndices.y].OnNumberClickCallback(number);
    }

    /// <summary>
    /// Callback that is executed when selecting numbers on the board using mouse clicks.
    /// </summary>
    /// <param name="number">Number selected for a cube in the board.</param>
    /// <param name="cubeIndices">Indices of a cube in the board.</param>
    private void OnCubeUpdateCallback(int number, Vector2Int cubeIndices)
    {
        BoardUpdateStack.Push(new BacktrackStateData()
        {
            CubeNumber = number,
            CubeIndices = cubeIndices
        });

        // Calculuate the start area indices associated with the specified cube
        Vector2Int startAreaIndices = new Vector2Int((cubeIndices.x / NumberOfAreasInBoard.x) * NumberOfAreasInBoard.x,
            (cubeIndices.y / NumberOfAreasInBoard.y) * NumberOfAreasInBoard.y);

        // Disable the selected cube
        CubeControllerMatrix[cubeIndices.x, cubeIndices.y].ToggleSpecificNumberControllerState(number, false);
        CubeControllerMatrix[cubeIndices.x, cubeIndices.y].UpdateAvailableNumbers(number, false, true, false);

        // Propagate the new cube number selection to surrounding cubes (area, row, column)
        ToggleNumbersInArea(cubeIndices, startAreaIndices, number, false);
        ToggleNumbersinRowsOfColumn(cubeIndices, startAreaIndices, number, false);
        ToggleNumbersInColumnsOfRow(cubeIndices, startAreaIndices, number, false);
    }

    public void RevertLastBoardUpdate()
    {
        BacktrackStateData lastBoardUpdate = BoardUpdateStack.Pop();

        // Calculuate the start area indices associated with the specified cube
        Vector2Int startAreaIndices = new Vector2Int((lastBoardUpdate.CubeIndices.x / NumberOfAreasInBoard.x) * NumberOfAreasInBoard.x,
            (lastBoardUpdate.CubeIndices.y / NumberOfAreasInBoard.y) * NumberOfAreasInBoard.y);

        // Re-enable/Reset the selected cube
        CubeControllerMatrix[lastBoardUpdate.CubeIndices.x, lastBoardUpdate.CubeIndices.y].ResetCubeController();
        RevertSelectedCubeChangeInCountMap(lastBoardUpdate.CubeIndices.x, lastBoardUpdate.CubeIndices.y);

        // Propagate the new cube number selection to surrounding cubes (area, row, column)
        ToggleNumbersInArea(lastBoardUpdate.CubeIndices, startAreaIndices, lastBoardUpdate.CubeNumber, true);
        ToggleNumbersinRowsOfColumn(lastBoardUpdate.CubeIndices, startAreaIndices, lastBoardUpdate.CubeNumber, true);
        ToggleNumbersInColumnsOfRow(lastBoardUpdate.CubeIndices, startAreaIndices, lastBoardUpdate.CubeNumber, true);
    }

    private void ToggleNumbersInArea(Vector2Int cubeIndices, Vector2Int startAreaIndices, int number, bool numberState)
    {
        /*
            Disable number in area of the cube indices.

            This is done by calculating the starting area indices associated with the cube indices,
            where integer division is used to find out the correct area and then multiplied by three
            to indicate the starting indices of the area.

            Lastly, we simply iterate up to the number of cubes per area per axis, and disable each cube.
        */

        for (int x = startAreaIndices.x; x < startAreaIndices.x + NumberOfCubesPerArea.x; x++)
        {
            for (int y = startAreaIndices.y; y < startAreaIndices.y + NumberOfCubesPerArea.y; y++)
            {
                if ((x != cubeIndices.x) || (y != cubeIndices.y))
                {
                    if (CubeControllerMatrix[x, y].CubeNumber == 0)
                    {
                        CubeControllerMatrix[x, y].ToggleSpecificNumberControllerState(number, numberState);
                    }
                    CubeControllerMatrix[x, y].UpdateAvailableNumbers(number, numberState, false, false);
                }
            }
        }
    }

    private void ToggleNumbersinRowsOfColumn(Vector2Int cubeIndices, Vector2Int startAreaIndices, int number, bool numberState)
    {
        // Disable number in cubes in all rows of indicated column
        for (int i = 0; i < CubeControllerMatrix.GetLength(0); i++)
        {
            // Don't update the selected cube itself - since the number has been disabled in the cube already
            if (cubeIndices.x != i)
            {
                // Only disable number in cubes outside of the area (which have been processed already)
                if ((i < startAreaIndices.x) || (i >= startAreaIndices.x + NumberOfCubesPerArea.x))
                {
                    if (CubeControllerMatrix[i, cubeIndices.y].CubeNumber == 0)
                    {
                        CubeControllerMatrix[i, cubeIndices.y].ToggleSpecificNumberControllerState(number, numberState);

                    }
                    CubeControllerMatrix[i, cubeIndices.y].UpdateAvailableNumbers(number, numberState, false, false);
                }
            }
        }
    }

    private void ToggleNumbersInColumnsOfRow(Vector2Int cubeIndices, Vector2Int startAreaIndices, int number, bool numberState)
    {
        // Disable number in cubes in all columns of indicated row
        for (int j = 0; j < CubeControllerMatrix.GetLength(1); j++)
        {
            // Don't update the selected cube itself - since the number has been disabled in the cube already
            if (cubeIndices.y != j)
            {
                // Only disable number in cubes outside of the area (which have been processed already)
                if ((j < startAreaIndices.y) || (j >= startAreaIndices.y + NumberOfCubesPerArea.y))
                {
                    if (CubeControllerMatrix[cubeIndices.x, j].CubeNumber == 0)
                    {
                        CubeControllerMatrix[cubeIndices.x, j].ToggleSpecificNumberControllerState(number, numberState);
                    }
                    CubeControllerMatrix[cubeIndices.x, j].UpdateAvailableNumbers(number, numberState, false, false);
                }
            }
        }
    }

    public void UpdateAvailableNumberCountMapSelect(Vector2Int cubeIndices, int prevCount, int newCount)
    {
        NumberCountToCubeListMap[prevCount].Remove(CubeControllerMatrix[cubeIndices.x, cubeIndices.y]);
        SelectedCubeList.Add(CubeControllerMatrix[cubeIndices.x, cubeIndices.y]);
    }

    public void UpdateAvailableNumberCountMapDeselect(Vector2Int cubeIndices, int prevCount, int newCount)
    {
        SelectedCubeList.Remove(CubeControllerMatrix[cubeIndices.x, cubeIndices.y]);
        NumberCountToCubeListMap[CubeControllerMatrix[cubeIndices.x, cubeIndices.y].
            CountAvailableNumbersForCube()].Add(CubeControllerMatrix[cubeIndices.x, cubeIndices.y]);
    }

    public void UpdateAvailableNumberCountMapPropagate(Vector2Int cubeIndices, int prevCount, int newCount)
    {
        NumberCountToCubeListMap[prevCount].Remove(CubeControllerMatrix[cubeIndices.x, cubeIndices.y]);
        NumberCountToCubeListMap[newCount].Add(CubeControllerMatrix[cubeIndices.x, cubeIndices.y]);
    }

    private void AddCubeToAvailableCountMap(int rowIndex, int colIndex)
    {
        CubeController cubeController = CubeControllerMatrix[rowIndex, colIndex];

        // If selected, put into selected list
        if (cubeController.CubeNumber != 0)
            SelectedCubeList.Add(cubeController);
        else
            NumberCountToCubeListMap[cubeController.CountAvailableNumbersForCube()].Add(cubeController);
    }

    private void RevertSelectedCubeChangeInCountMap(int rowIndex, int colIndex)
    {
        CubeController cubeController = CubeControllerMatrix[rowIndex, colIndex];
        SelectedCubeList.Remove(cubeController);
        NumberCountToCubeListMap[cubeController.CountAvailableNumbersForCube()].Add(cubeController);
    }

    public void ToggleNumberState(int rowIndex, int colIndex, int number, bool numberState)
    {
        CubeControllerMatrix[rowIndex, colIndex].ToggleSpecificNumberControllerState(number, numberState);
    }

    /// <summary>
    /// This method checks if there are any invalid cubes available, where
    /// the cube has no available numbers to be selected and the number has no value.
    /// </summary>
    /// <returns>True if board is valid. False if board invalid.</returns>
    public bool IsBoardValid()
    {
        if (NumberCountToCubeListMap[0].Count > 0)
            return false;
        return true;
    }

    /// <summary>
    /// This helper method is used to checked whether the board is complete and no longer needs
    /// to be processed.
    /// </summary>
    /// <returns>True if board complete. False if board not complete.</returns>
    public bool IsBoardComplete()
    {
        int counter = 0;

        for (int i = 0; i < CubeControllerMatrix.GetLength(0); i++)
            for (int j = 0; j < CubeControllerMatrix.GetLength(1); j++)
                if (CubeControllerMatrix[i, j].CubeNumber != 0)
                    counter++;

        return counter == (NumberOfAreasInBoard.x * NumberOfCubesPerArea.x * NumberOfAreasInBoard.y * NumberOfCubesPerArea.y);
    }

    public Vector2Int SelectLowestEntropyCube()
    {
        // Ignore 0 since we looking for cubes that do not yet have a number
        for (int i = 1; i < 10; i++)
        {
            if (NumberCountToCubeListMap[i].Count > 0)
            {
                int randomPosition = Random.Range(0, NumberCountToCubeListMap[i].Count);
                return NumberCountToCubeListMap[i][randomPosition].CubeIndices;
            }
        }

        return new Vector2Int(-1, -1);
    }
}
