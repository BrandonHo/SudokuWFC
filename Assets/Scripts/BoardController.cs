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

    public void InstantiateBoard(Vector2Int numberOfAreasInBoard, Vector2Int numberOfCubesPerArea, float offsetBetweenAreas, SudokuBoard boardData)
    {
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
                CubeControllerMatrix[i, j].AddOnCubeEventListener(OnCubeUpdateCallback);

                // Adjust column position for next cube
                cubePosition += new Vector3(1f, 0f, 0f);
            }

            // Reset column position + adjust row position for next row of cubes
            cubePosition = new Vector3(0f, 0f, cubePosition.z + 1f);
        }

        // A data structure that maps available number counts to a list of sudoku cubes that satisfy the corresponding counts
        AvailableNumbersCountToCubeListMap = new Dictionary<int, List<CubeController>>();
        for (int i = 0; i < 10; i++)
            AvailableNumbersCountToCubeListMap.Add(i, new List<CubeController>());
    }

    private void OnCubeUpdateCallback(int number, Vector2Int cubeIndices)
    {
        PropagateCubeUpdateChanges(number, cubeIndices);
    }

    private void PropagateCubeUpdateChanges(int number, Vector2Int cubeIndices)
    {
        // Calculuate the start area indices associated with the specified cube
        Vector2Int startAreaIndices = new Vector2Int((cubeIndices.x / NumberOfAreasInBoard.x) * NumberOfAreasInBoard.x,
            (cubeIndices.y / NumberOfAreasInBoard.y) * NumberOfAreasInBoard.y);

        // Propagate the new cube number selection to surrounding cubes (area, row, column)
        DisableNumbersInArea(number, cubeIndices, startAreaIndices);
        DisableNumbersInAllRowsOfColumn(number, cubeIndices, startAreaIndices);
        DisableNumbersInAllColumnsOfRow(number, cubeIndices, startAreaIndices);
    }

    private void DisableNumbersInArea(int number, Vector2Int cubeIndices, Vector2Int areaIndices)
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
                CubeControllerMatrix[x, y].DisableNumber(number);
                UpdateNumberCountMap(x, y);
            }
        }
    }

    private void DisableNumbersInAllRowsOfColumn(int number, Vector2Int cubeIndices, Vector2Int areaIndices)
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
                    CubeControllerMatrix[i, cubeIndices.y].DisableNumber(number);
                    UpdateNumberCountMap(i, cubeIndices.y);
                }
            }
        }
    }

    private void DisableNumbersInAllColumnsOfRow(int number, Vector2Int cubeIndices, Vector2Int areaIndices)
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
                    CubeControllerMatrix[cubeIndices.x, j].DisableNumber(number);
                    UpdateNumberCountMap(cubeIndices.x, j);
                }
            }
        }
    }

    /// <summary>
    /// Updates the available number count map appropriate in response to the selection
    /// of a cube number in the sudoku board.
    /// </summary>
    /// <param name="rowIndex">row of the updated cube in the sudoku board matrix</param>
    /// <param name="colIndex">column of the updated cube in the sudoku board matrix</param>
    private void UpdateNumberCountMap(int rowIndex, int colIndex)
    {
        /*
            The map is updated by moving the selected cube into the correct list.
            This is performed by:
            - Getting the current available number count of the specified cube
            - Checking if the cube exists in the list of the previous available number count (+1 of current count)
                - If exists, then remove it from old list and add to new list
        */

        int availableNumberCount = CubeControllerMatrix[rowIndex, colIndex].CountAvailableNumbersForCube();
        if (AvailableNumbersCountToCubeListMap[availableNumberCount + 1].Contains(CubeControllerMatrix[rowIndex, colIndex]))
        {
            AvailableNumbersCountToCubeListMap[availableNumberCount + 1].Remove(CubeControllerMatrix[rowIndex, colIndex]);
            AvailableNumbersCountToCubeListMap[availableNumberCount].Add(CubeControllerMatrix[rowIndex, colIndex]);
        }
    }
}
