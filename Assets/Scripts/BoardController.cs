using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class BoardController : MonoBehaviour
{
    public GameObject CubePrefab;
    private CubeController[,] CubeControllerMatrix;

    private Dictionary<int, List<CubeController>> NumberCountToCubeListMap;
    private List<CubeController> SelectedCubeList;

    private Vector2Int NumberOfAreasInBoard;
    private Vector2Int NumberOfCubesPerArea;

    public void InitialiseBoard(SudokuBoardDataAsset boardDataAsset, float offsetBetweenAreas)
    {
        NumberOfAreasInBoard = boardDataAsset.NumberOfAreasInBoard;
        NumberOfCubesPerArea = boardDataAsset.NumberOfCubesPerArea;

        // Calculate board dimensions + initialise matrix for storing cubes of the sudoku board
        Vector3 boardDimensions = new Vector3(NumberOfAreasInBoard.x * NumberOfCubesPerArea.x,
            0, NumberOfAreasInBoard.y * NumberOfCubesPerArea.y);
        CubeControllerMatrix = new CubeController[(int)boardDimensions.x, (int)boardDimensions.z];

        // Initial position of cube + offset vector for correcting the position of instantiated cubes
        Vector3 cubePosition = Vector3.zero + transform.position;
        Vector3 offsetCorrection = new Vector3(boardDimensions.x / 2 - (NumberOfAreasInBoard.x - 1) * (NumberOfAreasInBoard.y - 1) * offsetBetweenAreas,
            0f, boardDimensions.z / 2 - (NumberOfAreasInBoard.x - 1) * (NumberOfAreasInBoard.y - 1) * offsetBetweenAreas);

        for (int i = 0; i < CubeControllerMatrix.GetLength(0); i++)
        {
            // Add row offset between the areas
            if ((i != 0) && (i % NumberOfCubesPerArea.x == 0))
                cubePosition += new Vector3(0f, 0f, offsetBetweenAreas);

            for (int j = 0; j < CubeControllerMatrix.GetLength(1); j++)
            {
                // Add column offset between the areas
                if ((j != 0) && (j % NumberOfCubesPerArea.y == 0))
                    cubePosition += new Vector3(offsetBetweenAreas, 0f, 0f);

                // Instantiate cube + set parenting for the cubes
                GameObject newCube = Instantiate(CubePrefab, cubePosition - offsetCorrection, Quaternion.identity);
                newCube.transform.SetParent(transform);

                // Most importantly, reference the cube controller component in the matrix
                CubeControllerMatrix[i, j] = newCube.GetComponent<CubeController>();
                CubeControllerMatrix[i, j].Initialise();

                // Update cube indices + add callback for cube update events
                CubeControllerMatrix[i, j].SetCubeData(boardDataAsset.GetSudokuCubeData(i, j));

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

    public void SetupAvailableCountMapsForCubes()
    {
        NumberCountToCubeListMap = new Dictionary<int, List<CubeController>>();
        SelectedCubeList = new List<CubeController>();
        for (int x = 0; x < 10; x++)
            NumberCountToCubeListMap.Add(x, new List<CubeController>());

        for (int i = 0; i < CubeControllerMatrix.GetLength(0); i++)
        {
            for (int j = 0; j < CubeControllerMatrix.GetLength(1); j++)
            {
                // Identify invalid numbers for cube
                for (int aNumber = 1; aNumber <= 9; aNumber++)
                {
                    if (!IsNumberValidAtCubeIndices(i, j, aNumber))
                    {
                        CubeControllerMatrix[i, j].UpdateAvailableNumberState(aNumber, false);
                        CubeControllerMatrix[i, j].ToggleSpecificNumberControllerState(aNumber, false);
                    }
                }

                // Then add the cube to appropriate count list in map
                AddCubeToAvailableCountMap(CubeControllerMatrix[i, j]);
            }
        }
    }

    private bool IsNumberValidAtCubeIndices(int rowIndex, int colIndex, int number)
    {
        Vector2Int startAreaIndices = CalculateAreaStartIndicesFromCubeIndices(new Vector2Int(rowIndex, colIndex));

        if (DoesNumberExistInAreaOfCube(rowIndex, colIndex, startAreaIndices, number))
            return false;
        if (DoesNumberExistInColumnOfCube(rowIndex, colIndex, startAreaIndices, number))
            return false;
        if (DoesNumberExistInRowOfCube(rowIndex, colIndex, startAreaIndices, number))
            return false;

        return true;
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
        // Calculuate the start area indices associated with the specified cube
        Vector2Int startAreaIndices = CalculateAreaStartIndicesFromCubeIndices(cubeIndices);

        // Disable the selected cube
        CubeControllerMatrix[cubeIndices.x, cubeIndices.y].ToggleSpecificNumberControllerState(number, false);
        CubeControllerMatrix[cubeIndices.x, cubeIndices.y].UpdateAvailableNumbers(number, false, true, false);

        // Propagate the new cube number selection to surrounding cubes (area, row, column)
        ToggleNumbersInArea(cubeIndices, startAreaIndices, number, false);
        ToggleNumbersinRowsOfColumn(cubeIndices, startAreaIndices, number, false);
        ToggleNumbersInColumnsOfRow(cubeIndices, startAreaIndices, number, false);
    }

    private Vector2Int CalculateAreaStartIndicesFromCubeIndices(Vector2Int cubeIndices)
    {
        return new Vector2Int((cubeIndices.x / NumberOfAreasInBoard.x) * NumberOfAreasInBoard.x,
            (cubeIndices.y / NumberOfAreasInBoard.y) * NumberOfAreasInBoard.y);
    }

    public void RevertCubeUpdate(Vector2Int cubeIndices, int cubeNumber, Dictionary<int, List<BacktrackCubeData>> invalidCubeData)
    {
        // Calculuate the start area indices associated with the specified cube
        Vector2Int startAreaIndices = CalculateAreaStartIndicesFromCubeIndices(cubeIndices);

        // Re-enable/Reset the selected cube
        CubeControllerMatrix[cubeIndices.x, cubeIndices.y].ResetCubeController();
        RevertSelectedCubeChangeInCountMap(cubeIndices.x, cubeIndices.y);

        // Propagate the new cube number selection to surrounding cubes (area, row, column)
        EnableNumberInAreaOfCube(cubeIndices, startAreaIndices, cubeNumber, invalidCubeData);
        EnableNumberInColumnOfCube(cubeIndices, startAreaIndices, cubeNumber, invalidCubeData);
        EnableNumberInRowOfCube(cubeIndices, startAreaIndices, cubeNumber, invalidCubeData);
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

    private void EnableNumberInAreaOfCube(Vector2Int cubeIndices, Vector2Int startAreaIndices, int number, Dictionary<int, List<BacktrackCubeData>> invalidCubeData)
    {
        for (int x = startAreaIndices.x; x < startAreaIndices.x + NumberOfCubesPerArea.x; x++)
        {
            for (int y = startAreaIndices.y; y < startAreaIndices.y + NumberOfCubesPerArea.y; y++)
            {
                // Ignore the "target" cube itself
                if ((x != cubeIndices.x) || (y != cubeIndices.y))
                {
                    if (!IsCubeUpdateInvalid(CubeControllerMatrix[x, y], invalidCubeData))
                    {
                        if ((!DoesNumberExistInColumnOfCube(x, y, startAreaIndices, number)) && (!DoesNumberExistInRowOfCube(x, y, startAreaIndices, number)))
                        {
                            if (CubeControllerMatrix[x, y].CubeNumber == 0)
                                CubeControllerMatrix[x, y].ToggleSpecificNumberControllerState(number, true);
                            CubeControllerMatrix[x, y].UpdateAvailableNumbers(number, true, false, false);
                        }
                    }
                }
            }
        }
    }

    private void EnableNumberInColumnOfCube(Vector2Int cubeIndices, Vector2Int startAreaIndices, int number, Dictionary<int, List<BacktrackCubeData>> invalidCubeData)
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
                    if (!IsCubeUpdateInvalid(CubeControllerMatrix[i, cubeIndices.y], invalidCubeData))
                    {
                        Vector2Int startAreaIndicesForSelectedCube = CalculateAreaStartIndicesFromCubeIndices(new Vector2Int(i, cubeIndices.y));

                        if ((!DoesNumberExistInAreaOfCube(i, cubeIndices.y, startAreaIndicesForSelectedCube, number)))
                        {
                            if (!DoesNumberExistInRowOfCube(i, cubeIndices.y, startAreaIndicesForSelectedCube, number))
                            {
                                if (CubeControllerMatrix[i, cubeIndices.y].CubeNumber == 0)
                                    CubeControllerMatrix[i, cubeIndices.y].ToggleSpecificNumberControllerState(number, true);
                                CubeControllerMatrix[i, cubeIndices.y].UpdateAvailableNumbers(number, true, false, false);
                            }
                        }
                    }
                }
            }
        }
    }

    private void EnableNumberInRowOfCube(Vector2Int cubeIndices, Vector2Int startAreaIndices, int number, Dictionary<int, List<BacktrackCubeData>> invalidCubeData)
    {
        for (int j = 0; j < CubeControllerMatrix.GetLength(0); j++)
        {
            // Ignore the "target" cube itself
            if (cubeIndices.y != j)
            {
                // Only consider cubes outside the area
                if ((j < startAreaIndices.y) || (j >= startAreaIndices.y + NumberOfCubesPerArea.y))
                {
                    if (!IsCubeUpdateInvalid(CubeControllerMatrix[cubeIndices.x, j], invalidCubeData))
                    {
                        Vector2Int startAreaIndicesForSelectedCube = CalculateAreaStartIndicesFromCubeIndices(new Vector2Int(cubeIndices.x, j));

                        if ((!DoesNumberExistInAreaOfCube(cubeIndices.x, j, startAreaIndicesForSelectedCube, number)))
                        {
                            if (!DoesNumberExistInColumnOfCube(cubeIndices.x, j, startAreaIndicesForSelectedCube, number))
                            {
                                if (CubeControllerMatrix[cubeIndices.x, j].CubeNumber == 0)
                                    CubeControllerMatrix[cubeIndices.x, j].ToggleSpecificNumberControllerState(number, true);
                                CubeControllerMatrix[cubeIndices.x, j].UpdateAvailableNumbers(number, true, false, false);
                            }
                        }
                    }
                }
            }
        }
    }

    private bool IsCubeUpdateInvalid(CubeController cubeController, Dictionary<int, List<BacktrackCubeData>> invalidCubeData)
    {
        // Check if the cube number + cube indices match any cube data in the invalid cube data map
        if (invalidCubeData.ContainsKey(cubeController.CubeNumber))
            for (int x = 0; x < invalidCubeData[cubeController.CubeNumber].Count; x++)
                if ((invalidCubeData[cubeController.CubeNumber][x].CubeIndices.x == cubeController.CubeIndices.x)
                && (invalidCubeData[cubeController.CubeNumber][x].CubeIndices.y == cubeController.CubeIndices.y))
                    return true;

        return false;
    }

    private bool DoesNumberExistInAreaOfCube(int rowIndex, int colIndex, Vector2Int startAreaIndices, int number)
    {
        for (int x = startAreaIndices.x; x < startAreaIndices.x + NumberOfCubesPerArea.x; x++)
        {
            for (int y = startAreaIndices.y; y < startAreaIndices.y + NumberOfCubesPerArea.y; y++)
            {
                // Ignore the "target" cube itself
                if ((x != rowIndex) || (y != colIndex))
                {
                    if (CubeControllerMatrix[x, y].CubeNumber == number)
                        return true;
                }
            }
        }

        return false;
    }

    private bool DoesNumberExistInColumnOfCube(int rowIndex, int colIndex, Vector2Int startAreaIndices, int number)
    {
        for (int i = 0; i < CubeControllerMatrix.GetLength(0); i++)
        {
            // Ignore the "target" cube itself
            if (rowIndex != i)
            {
                // Only consider cubes outside the area
                if ((i < startAreaIndices.x) || (i >= startAreaIndices.x + NumberOfCubesPerArea.x))
                {
                    if (CubeControllerMatrix[i, colIndex].CubeNumber == number)
                        return true;
                }
            }
        }

        return false;
    }

    private bool DoesNumberExistInRowOfCube(int rowIndex, int colIndex, Vector2Int startAreaIndices, int number)
    {
        for (int j = 0; j < CubeControllerMatrix.GetLength(1); j++)
        {
            // Ignore the "target" cube itself
            if (colIndex != j)
            {
                // Only consider cubes outside the area
                if ((j < startAreaIndices.y) || (j >= startAreaIndices.y + NumberOfCubesPerArea.y))
                {
                    if (CubeControllerMatrix[rowIndex, j].CubeNumber == number)
                        return true;
                }
            }
        }

        return false;
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

    private void AddCubeToAvailableCountMap(CubeController cubeController)
    {
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

    public CubeController SelectLowestEntropyCube()
    {
        // Ignore 0 since we looking for cubes that do not yet have a number
        for (int i = 1; i < 10; i++)
        {
            if (NumberCountToCubeListMap[i].Count > 0)
            {
                int randomPosition = Random.Range(0, NumberCountToCubeListMap[i].Count);
                return NumberCountToCubeListMap[i][randomPosition];
            }
        }

        return null;
    }
}
