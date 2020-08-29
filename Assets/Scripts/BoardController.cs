using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class BoardController : MonoBehaviour
{
    public GameObject CubePrefab;

    private CubeController[,] CubeControllerMatrix;
    /// <summary>
    /// This data structure maps available number counts to various cubes in the board.
    /// This enables quick queries for cubes that satisfy specific available number counts.
    /// For example, finding sudoku cubes with only one number available for selection.
    /// </summary>
    private Dictionary<int, List<CubeController>> AvailableNumbersCountToCubeListMap;
    private Vector2Int NumberOfAreasInBoard;
    private Vector2Int NumberOfCubesPerArea;

    public UnityEventCubeUpdate OnCubeUpdateEvent;

    void Awake()
    {
        OnCubeUpdateEvent = new UnityEventCubeUpdate();
    }

    public void InstantiateBoard(Vector2Int numberOfAreasInBoard, Vector2Int numberOfCubesPerArea, float offsetBetweenAreas, SudokuBoard boardData)
    {
        // A data structure that maps available number counts to a list of sudoku cubes that satisfy the corresponding counts
        AvailableNumbersCountToCubeListMap = new Dictionary<int, List<CubeController>>();
        for (int i = 0; i < 10; i++)
            AvailableNumbersCountToCubeListMap.Add(i, new List<CubeController>());

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
                AvailableNumbersCountToCubeListMap[CubeControllerMatrix[i, j].CountAvailableNumbersForCube()].Add(CubeControllerMatrix[i, j]);
                CubeControllerMatrix[i, j].AddOnCubeEventListener(OnCubeUpdateCallback);
                CubeControllerMatrix[i, j].AddOnAvailableNumbersUpdateEventListener(UpdateNumberCountMap);

                // Adjust column position for next cube
                cubePosition += new Vector3(1f, 0f, 0f);
            }

            // Reset column position + adjust row position for next row of cubes
            cubePosition = new Vector3(0f, 0f, cubePosition.z + 1f);
        }
    }

    private void OnCubeUpdateCallback(int number, Vector2Int cubeIndices)
    {
        if (OnCubeUpdateEvent != null)
            OnCubeUpdateEvent.Invoke(number, cubeIndices);

        PropagateCubeUpdateChanges(number, cubeIndices);
    }

    public void AddListenerToCubeUpdateEvent(UnityAction<int, Vector2Int> callback)
    {
        if (OnCubeUpdateEvent != null)
            OnCubeUpdateEvent.AddListener(callback);
    }

    public void DisableNumberState(Vector2Int cubeIndices, int number, bool numberState, bool isSelected)
    {
        CubeControllerMatrix[cubeIndices.x, cubeIndices.y].ToggleNumberState(number, numberState);
        CubeControllerMatrix[cubeIndices.x, cubeIndices.y].RemoveNumberFromAvailableNumbersArray(number, numberState, isSelected);
    }

    public void DisableNumberState(int rowIndex, int colIndex, int number, bool numberState, bool isSelected)
    {
        CubeControllerMatrix[rowIndex, colIndex].ToggleNumberState(number, numberState);
        CubeControllerMatrix[rowIndex, colIndex].RemoveNumberFromAvailableNumbersArray(number, numberState, isSelected);
    }

    private void PropagateCubeUpdateChanges(int number, Vector2Int cubeIndices)
    {
        // Calculuate the start area indices associated with the specified cube
        Vector2Int startAreaIndices = new Vector2Int((cubeIndices.x / NumberOfAreasInBoard.x) * NumberOfAreasInBoard.x,
            (cubeIndices.y / NumberOfAreasInBoard.y) * NumberOfAreasInBoard.y);

        // Disable selected number from callback
        DisableNumberState(cubeIndices, number, false, true);

        // Propagate the new cube number selection to surrounding cubes (area, row, column)
        DisableNumbersInArea(number, cubeIndices, startAreaIndices, false);
        DisableNumbersinRowsOfColumn(number, cubeIndices, startAreaIndices, false);
        DisableNumbersInColumnsOfRow(number, cubeIndices, startAreaIndices, false);
    }

    private void DisableNumbersInArea(int number, Vector2Int cubeIndices, Vector2Int areaIndices, bool enableNumber)
    {
        /*
            Disable number in area of the cube indices.

            This is done by calculating the starting area indices associated with the cube indices,
            where integer division is used to find out the correct area and then multiplied by three
            to indicate the starting indices of the area.

            Lastly, we simply iterate up to the number of cubes per area per axis, and disable each cube.
        */

        for (int x = areaIndices.x; x < areaIndices.x + NumberOfCubesPerArea.x; x++)
        {
            for (int y = areaIndices.y; y < areaIndices.y + NumberOfCubesPerArea.y; y++)
            {
                if ((x != cubeIndices.x) || (y != cubeIndices.y))
                {
                    DisableNumberState(x, y, number, false, false);
                }
            }
        }
    }

    private void DisableNumbersinRowsOfColumn(int number, Vector2Int cubeIndices, Vector2Int areaIndices, bool enableNumber)
    {
        // Disable number in cubes in all rows of indicated column
        for (int i = 0; i < CubeControllerMatrix.GetLength(0); i++)
        {
            // Don't update the selected cube itself - since the number has been disabled in the cube already
            if (cubeIndices.x != i)
            {
                // Only disable number in cubes outside of the area (which have been processed already)
                if ((i < areaIndices.x) || (i >= areaIndices.x + NumberOfCubesPerArea.x))
                {
                    DisableNumberState(i, cubeIndices.y, number, false, false);
                }
            }
        }
    }

    private void DisableNumbersInColumnsOfRow(int number, Vector2Int cubeIndices, Vector2Int areaIndices, bool enableNumber)
    {
        // Disable number in cubes in all columns of indicated row
        for (int j = 0; j < CubeControllerMatrix.GetLength(1); j++)
        {
            // Don't update the selected cube itself - since the number has been disabled in the cube already
            if (cubeIndices.y != j)
            {
                // Only disable number in cubes outside of the area (which have been processed already)
                if ((j < areaIndices.y) || (j >= areaIndices.y + NumberOfCubesPerArea.y))
                {
                    DisableNumberState(cubeIndices.x, j, number, false, false);
                }
            }
        }
    }

    /// <summary>
    /// Updates the available number count map appropriate in response to the selection
    /// of a cube number in the sudoku board.
    /// </summary>
    private void UpdateNumberCountMap(bool enableNumber, Vector2Int cubeIndices, bool newlySelected)
    {
        /*
            The map is updated by moving the selected cube into the correct list.
            This is performed by:
            - Getting the current available number count of the specified cube
            - Checking if the cube exists in the list of the previous available number count (+1 of current count)
                - If exists, then remove it from old list and add to new list
        */

        int availableNumberCount = CubeControllerMatrix[cubeIndices.x, cubeIndices.y].CountAvailableNumbersForCube();

        // If disabling number -> need to check if we disabling as a result of propagation or if cube was selected
        if (!enableNumber)
        {
            if (newlySelected)
            {
                // Should always exist because only one propagation takes place at a time
                AvailableNumbersCountToCubeListMap[availableNumberCount + 1].Remove(CubeControllerMatrix[cubeIndices.x, cubeIndices.y]);
                AvailableNumbersCountToCubeListMap[0].Add(CubeControllerMatrix[cubeIndices.x, cubeIndices.y]);
            }
            else if (CubeControllerMatrix[cubeIndices.x, cubeIndices.y].CubeNumber == 0)
            {
                // Should always exist because only one propagation takes place at a time
                AvailableNumbersCountToCubeListMap[availableNumberCount + 1].Remove(CubeControllerMatrix[cubeIndices.x, cubeIndices.y]);
                AvailableNumbersCountToCubeListMap[availableNumberCount].Add(CubeControllerMatrix[cubeIndices.x, cubeIndices.y]);
            }
        }
    }

    /// <summary>
    /// This method checks if there are any invalid cubes available, where
    /// the cube has no available numbers to be selected and the number has no value.
    /// </summary>
    /// <returns>True if board is valid. False if board invalid.</returns>
    public bool IsBoardValid(out Vector2Int invalidCubeIndices)
    {
        for (int i = 0; i < AvailableNumbersCountToCubeListMap[0].Count; i++)
        {
            if (AvailableNumbersCountToCubeListMap[0][i].CubeNumber == 0)
            {
                invalidCubeIndices = AvailableNumbersCountToCubeListMap[0][i].CubeIndices;
                return false;
            }
        }

        invalidCubeIndices = new Vector2Int(-1, -1);
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

    public Vector2Int SelectLowestEntropyCube(out int availableNumberCount)
    {
        availableNumberCount = 0;

        // Ignore 0 since we looking for cubes that do not yet have a number
        for (int i = 1; i < 10; i++)
        {
            if (AvailableNumbersCountToCubeListMap[i].Count > 0)
            {
                availableNumberCount = i;
                int randomPosition = Random.Range(0, AvailableNumbersCountToCubeListMap[i].Count);
                return AvailableNumbersCountToCubeListMap[i][randomPosition].CubeIndices;
            }
        }

        return new Vector2Int(-1, -1);
    }

    public void SelectNumberForCube(int number, Vector2Int cubeIndices)
    {
        CubeControllerMatrix[cubeIndices.x, cubeIndices.y].OnNumberClickCallback(number);
    }

    public void ReverseAvailableCountMapUpdate(Vector2Int cubeIndices, bool wasSelected)
    {
        int availableNumberCount = CubeControllerMatrix[cubeIndices.x, cubeIndices.y].CountAvailableNumbersForCube();

        if (wasSelected)
        {
            AvailableNumbersCountToCubeListMap[0].Remove(CubeControllerMatrix[cubeIndices.x, cubeIndices.y]);
            AvailableNumbersCountToCubeListMap[availableNumberCount].Add(CubeControllerMatrix[cubeIndices.x, cubeIndices.y]);
        }
    }

    public void UpdateBoardWithNewData(SudokuCubeData[,] cubeDataMatrix)
    {
        for (int i = 0; i < CubeControllerMatrix.GetLength(0); i++)
        {
            for (int j = 0; j < CubeControllerMatrix.GetLength(1); j++)
            {
                CubeControllerMatrix[i, j].SetCubeData(cubeDataMatrix[i, j]);
                CubeControllerMatrix[i, j].RefreshCubeController();
            }
        }
    }
}
