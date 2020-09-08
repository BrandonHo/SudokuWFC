using UnityEngine;
using System.Collections.Generic;

public class BoardController : MonoBehaviour
{
    public GameObject CubePrefab;                                           // Prefab for representing cubes in the sudoku board
    private CubeController[,] CubeControllerMatrix;                         // Jagged array that contains references to cube controllers in the sudoku board
    private Dictionary<int, List<CubeController>> NumberCountToCubeListMap; // Map that is used to query for cube controllers with specific counts
    private List<CubeController> SelectedCubeList;                          // List that contains all cube controllers that have a number (selected)
    private Vector2Int NumberOfAreasInBoard;                                // Vector that describes the number of areas in the sudoku board
    private Vector2Int NumberOfCubesPerArea;                                // Vector that describes the number of cubes per area in the sudoku board

    /// <summary>
    /// Method that instantiates the cube game objects for the sudoku board.
    /// </summary>
    /// <param name="boardDataAsset">Asset that contains sudoku board data.</param>
    /// <param name="offsetBetweenAreas">Offset distance between areas in the sudoku board.</param>
    public void CreateSudokuBoard(SudokuBoardDataAsset boardDataAsset, float offsetBetweenAreas)
    {
        NumberOfAreasInBoard = boardDataAsset.NumberOfAreasInBoard;
        NumberOfCubesPerArea = boardDataAsset.NumberOfCubesPerArea;

        // Calculate board dimensions + initialise matrix for storing cubes of the sudoku board
        Vector3 boardDimensions = new Vector3(NumberOfAreasInBoard.x * NumberOfCubesPerArea.x, 0, NumberOfAreasInBoard.y * NumberOfCubesPerArea.y);
        CubeControllerMatrix = new CubeController[(int)boardDimensions.x, (int)boardDimensions.z];

        // Calculate initial position of cube + offset vector for correcting the position of instantiated cube game objects
        Vector3 cubePosition = Vector3.zero + transform.position;
        Vector3 offsetCorrection = new Vector3(boardDimensions.x / 2 - (NumberOfAreasInBoard.x - 1) * (NumberOfAreasInBoard.y - 1) * offsetBetweenAreas,
            0f, boardDimensions.z / 2 - (NumberOfAreasInBoard.x - 1) * (NumberOfAreasInBoard.y - 1) * offsetBetweenAreas);

        for (int i = 0; i < CubeControllerMatrix.GetLength(0); i++)
        {
            // Add row offset between areas - if needed
            if ((i != 0) && (i % NumberOfCubesPerArea.x == 0))
                cubePosition += new Vector3(0f, 0f, offsetBetweenAreas);

            for (int j = 0; j < CubeControllerMatrix.GetLength(1); j++)
            {
                // Add column offset between areas - if needed
                if ((j != 0) && (j % NumberOfCubesPerArea.y == 0))
                    cubePosition += new Vector3(offsetBetweenAreas, 0f, 0f);

                // Instantiate + set parenting for the cube game objects
                GameObject newCube = Instantiate(CubePrefab, cubePosition - offsetCorrection, Quaternion.identity);
                newCube.transform.SetParent(transform);

                // Reference, initialise, and update cube with appropriate data
                CubeControllerMatrix[i, j] = newCube.GetComponent<CubeController>();
                CubeControllerMatrix[i, j].Initialise();
                CubeControllerMatrix[i, j].SetCubeData(boardDataAsset.GetSudokuCubeData(i, j));

                // Add listeners to appropriate events 
                CubeControllerMatrix[i, j].AddOnCubeEventListener(OnCubeUpdateCallback);
                CubeControllerMatrix[i, j].AddOnAvailableNumbersUpdateEventListener(UpdateAvailableNumberCountMapSelect, true, false);
                CubeControllerMatrix[i, j].AddOnAvailableNumbersUpdateEventListener(UpdateAvailableNumberCountMapPropagate, false, false);

                // Adjust column position for next cube
                cubePosition += new Vector3(1f, 0f, 0f);
            }

            // Reset column position + adjust row position for next row of cubes
            cubePosition = new Vector3(0f, 0f, cubePosition.z + 1f);
        }
    }

    /// <summary>
    /// Method that initialises the available count maps for tracking cube game objects.
    /// This method should be called directly after the initialise board method.
    /// </summary>
    public void SetupAvailableCountMapsForCubes()
    {
        // Initialise available number count map + selected cube list
        NumberCountToCubeListMap = new Dictionary<int, List<CubeController>>();
        SelectedCubeList = new List<CubeController>();
        for (int x = 0; x < 10; x++)
            NumberCountToCubeListMap.Add(x, new List<CubeController>());

        // Cubes are initially instantiated with every number available - therefore we need to check which ones are invalid
        for (int i = 0; i < CubeControllerMatrix.GetLength(0); i++)
        {
            for (int j = 0; j < CubeControllerMatrix.GetLength(1); j++)
            {
                // Identify invalid numbers for cubes
                for (int aNumber = 1; aNumber <= 9; aNumber++)
                {
                    // If number invalid -  notify cube and disable the appropriate number controller
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

    /// <summary>
    /// Helper method for checking whether a number can exist at the specified indices in the sudoku board.
    /// </summary>
    /// <param name="rowIndex">Index for row in the sudoku board.</param>
    /// <param name="colIndex">Index for column in the sudoku board.</param>
    /// <param name="number">The number that is validated at the indices in the sudoku board.</param>
    /// <returns>True if number can exist at specified cube indices, false otherwise.</returns>
    private bool IsNumberValidAtCubeIndices(int rowIndex, int colIndex, int number)
    {
        Vector2Int startAreaIndices = CalculateAreaStartIndicesFromCubeIndices(new Vector2Int(rowIndex, colIndex));

        if (DoesNumberExistInArea(rowIndex, colIndex, startAreaIndices, number))
            return false;
        if (DoesNumberExistInColumn(rowIndex, colIndex, startAreaIndices, number))
            return false;
        if (DoesNumberExistInRow(rowIndex, colIndex, startAreaIndices, number))
            return false;

        return true;
    }

    /// <summary>
    /// Helper method for calculating the starting indices of an area for specific cube indices.
    /// </summary>
    /// <param name="cubeIndices">Cube indices for calculating the starting area indices.</param>
    /// <returns>The starting area indices of the specified cube indices.</returns>
    private Vector2Int CalculateAreaStartIndicesFromCubeIndices(Vector2Int cubeIndices)
    {
        return new Vector2Int((cubeIndices.x / NumberOfAreasInBoard.x) * NumberOfAreasInBoard.x,
            (cubeIndices.y / NumberOfAreasInBoard.y) * NumberOfAreasInBoard.y);
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
        CubeControllerMatrix[cubeIndices.x, cubeIndices.y].UpdateAvailableNumbers(number, false, true);

        // Propagate the new cube number selection to surrounding cubes (area, row, column)
        DisableNumberInArea(cubeIndices, startAreaIndices, number);
        DisableNumberInColumn(cubeIndices, startAreaIndices, number);
        DisableNumberInRow(cubeIndices, startAreaIndices, number);
    }

    /// <summary>
    /// Method for disabling specific number controllers of cube game objects within a specific area of the sudoku board.
    /// </summary>
    /// <param name="cubeIndices">Indices of the selected cube game object in the sudoku board.</param>
    /// <param name="startAreaIndices">Starting indices of the area associated with the selected cube game object.</param>
    /// <param name="number">The number identifying number controllers of cube game objects to be disabled in a specified area of the sudoku board.</param>
    private void DisableNumberInArea(Vector2Int cubeIndices, Vector2Int startAreaIndices, int number)
    {
        // Iterate through indices of the area associated with the cube
        for (int x = startAreaIndices.x; x < startAreaIndices.x + NumberOfCubesPerArea.x; x++)
        {
            for (int y = startAreaIndices.y; y < startAreaIndices.y + NumberOfCubesPerArea.y; y++)
            {
                // Don't toggle state of the cube itself in the area
                if ((x != cubeIndices.x) || (y != cubeIndices.y))
                {
                    // Toggle number controller state associated with number + update count map
                    if (CubeControllerMatrix[x, y].CubeNumber == 0)
                        CubeControllerMatrix[x, y].ToggleSpecificNumberControllerState(number, false);
                    CubeControllerMatrix[x, y].UpdateAvailableNumbers(number, false, false);
                }
            }
        }
    }

    /// <summary>
    /// Method for disabling specific number controllers of cube game objects within a specific column of the sudoku board.
    /// </summary>
    /// <param name="cubeIndices">Indices of the selected cube game object in the sudoku board.</param>
    /// <param name="startAreaIndices">Starting indices of the area associated with the selected cube game object.</param>
    /// <param name="number">The number identifying number controllers of cube game objects to be disabled in a specified column of the sudoku board.</param>
    private void DisableNumberInColumn(Vector2Int cubeIndices, Vector2Int startAreaIndices, int number)
    {
        for (int i = 0; i < CubeControllerMatrix.GetLength(0); i++)
        {
            // Don't update the selected cube itself - since the number has been handled in the cube already
            if (cubeIndices.x != i)
            {
                // Only disable number in cubes outside of the area (which have been processed already)
                if ((i < startAreaIndices.x) || (i >= startAreaIndices.x + NumberOfCubesPerArea.x))
                {
                    // Toggle number controller state associated with number + update count map
                    if (CubeControllerMatrix[i, cubeIndices.y].CubeNumber == 0)
                        CubeControllerMatrix[i, cubeIndices.y].ToggleSpecificNumberControllerState(number, false);
                    CubeControllerMatrix[i, cubeIndices.y].UpdateAvailableNumbers(number, false, false);
                }
            }
        }
    }

    /// <summary>
    /// Method for disabling specific number controllers of cube game objects within a specific row of the sudoku board.
    /// </summary>
    /// <param name="cubeIndices">Indices of the selected cube game object in the sudoku board.</param>
    /// <param name="startAreaIndices">Starting indices of the area associated with the selected cube game object.</param>
    /// <param name="number">The number identifying number controllers of cube game objects to be disabled in a specified row of the sudoku board.</param>
    private void DisableNumberInRow(Vector2Int cubeIndices, Vector2Int startAreaIndices, int number)
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
                        CubeControllerMatrix[cubeIndices.x, j].ToggleSpecificNumberControllerState(number, false);
                    CubeControllerMatrix[cubeIndices.x, j].UpdateAvailableNumbers(number, false, false);
                }
            }
        }
    }

    /// <summary>
    /// Method that is called to revert a specific cube update in the sudoku board.
    /// </summary>
    /// <param name="cubeIndices">Indices of the cube that was updated.</param>
    /// <param name="cubeNumber">Number associated with the cube that was updated.</param>
    /// <param name="invalidCubeData">A list containing all invalid cube data that was obtained from backtracking.</param>
    public void RevertCubeUpdate(Vector2Int cubeIndices, int cubeNumber, List<BacktrackCubeData> invalidCubeData)
    {
        // Calculuate the start area indices associated with the specified cube
        Vector2Int startAreaIndices = CalculateAreaStartIndicesFromCubeIndices(cubeIndices);

        // Re-enable/Reset the selected cube + adjust available count map
        CubeControllerMatrix[cubeIndices.x, cubeIndices.y].ResetCubeController();
        SelectedCubeList.Remove(CubeControllerMatrix[cubeIndices.x, cubeIndices.y]);
        NumberCountToCubeListMap[CubeControllerMatrix[cubeIndices.x, cubeIndices.y].CountAvailableNumbersForCube()]
            .Add(CubeControllerMatrix[cubeIndices.x, cubeIndices.y]);

        // Propagate the new cube number selection to surrounding cubes (area, row, column)
        EnableNumberInArea(cubeIndices, startAreaIndices, cubeNumber, invalidCubeData);
        EnableNumberInColumn(cubeIndices, startAreaIndices, cubeNumber, invalidCubeData);
        EnableNumberInRow(cubeIndices, startAreaIndices, cubeNumber, invalidCubeData);
    }

    /// <summary>
    /// Method for enabling specific number controllers of cube game objects in a specific area of the sudoku board.
    /// </summary>
    /// <param name="cubeIndices">Indices of the re-enabled cube game object in the sudoku board.</param>
    /// <param name="startAreaIndices">Starting indices of the area associated with the re-enabled cube game object.</param>
    /// <param name="number">The number identifying number controllers of cube game objects to be enabled in an area in sudoku board.</param>
    /// <param name="invalidCubeData">List consisting of invalid cube data found from backtracking.</param>
    private void EnableNumberInArea(Vector2Int cubeIndices, Vector2Int startAreaIndices, int number, List<BacktrackCubeData> invalidCubeData)
    {
        // Iterate through all cube controllers within the area
        for (int x = startAreaIndices.x; x < startAreaIndices.x + NumberOfCubesPerArea.x; x++)
        {
            for (int y = startAreaIndices.y; y < startAreaIndices.y + NumberOfCubesPerArea.y; y++)
            {
                // Ignore the re-enabled cube game object itself
                if ((x != cubeIndices.x) || (y != cubeIndices.y))
                {
                    // Should number remain disabled based on previous backtracking information?
                    if (!ShouldCubeRemainDisabled(CubeControllerMatrix[x, y], invalidCubeData))
                    {
                        // Only re-enable cube controller if appropriate based on column and row restrictions
                        if ((!DoesNumberExistInColumn(x, y, startAreaIndices, number)) && (!DoesNumberExistInRow(x, y, startAreaIndices, number)))
                        {
                            if (CubeControllerMatrix[x, y].CubeNumber == 0)
                                CubeControllerMatrix[x, y].ToggleSpecificNumberControllerState(number, true);
                            CubeControllerMatrix[x, y].UpdateAvailableNumbers(number, true, false);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Method for enabling specific number controllers of cube game objects in a specific column of the sudoku board.
    /// </summary>
    /// <param name="cubeIndices">Indices of the re-enabled cube game object in the sudoku board.</param>
    /// <param name="startAreaIndices">Starting indices of the area associated with the re-enabled cube game object.</param>
    /// <param name="number">The number identifying number controllers of cube game objects to be enabled in the column in sudoku board.</param>
    /// <param name="invalidCubeData">List consisting of invalid cube data found from backtracking.</param>
    private void EnableNumberInColumn(Vector2Int cubeIndices, Vector2Int startAreaIndices, int number, List<BacktrackCubeData> invalidCubeData)
    {
        // Iterate through cube controllers for all rows in the sudoku board
        for (int i = 0; i < CubeControllerMatrix.GetLength(0); i++)
        {
            // Ignore the re-enabled cube game object itself (which has been processed already)
            if (cubeIndices.x != i)
            {
                // Only enable number in cubes outside of the area (which have been processed already)
                if ((i < startAreaIndices.x) || (i >= startAreaIndices.x + NumberOfCubesPerArea.x))
                {
                    // Should number remain disabled based on previous backtracking information?
                    if (!ShouldCubeRemainDisabled(CubeControllerMatrix[i, cubeIndices.y], invalidCubeData))
                    {
                        // Only re-enable cube controller if appropriate based on area and row restrictions
                        Vector2Int startAreaIndicesForSelectedCube = CalculateAreaStartIndicesFromCubeIndices(new Vector2Int(i, cubeIndices.y));
                        if ((!DoesNumberExistInArea(i, cubeIndices.y, startAreaIndicesForSelectedCube, number)))
                        {
                            if (!DoesNumberExistInRow(i, cubeIndices.y, startAreaIndicesForSelectedCube, number))
                            {
                                if (CubeControllerMatrix[i, cubeIndices.y].CubeNumber == 0)
                                    CubeControllerMatrix[i, cubeIndices.y].ToggleSpecificNumberControllerState(number, true);
                                CubeControllerMatrix[i, cubeIndices.y].UpdateAvailableNumbers(number, true, false);
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Method for enabling specific number controllers of cube game objects in a specific row of the sudoku board.
    /// </summary>
    /// <param name="cubeIndices">Indices of the re-enabled cube game object in the sudoku board.</param>
    /// <param name="startAreaIndices">Starting indices of the area associated with the re-enabled cube game object.</param>
    /// <param name="number">The number identifying number controllers of cube game objects to be enabled in the row in sudoku board.</param>
    /// <param name="invalidCubeData">List consisting of invalid cube data found from backtracking.</param>
    private void EnableNumberInRow(Vector2Int cubeIndices, Vector2Int startAreaIndices, int number, List<BacktrackCubeData> invalidCubeData)
    {
        // Iterate through cube controllers for all columns of the sudoku board
        for (int j = 0; j < CubeControllerMatrix.GetLength(0); j++)
        {
            // Ignore the re-enabled cube game object itself (which has been processed already)
            if (cubeIndices.y != j)
            {
                // Only enable number in cubes outside of the area (which have been processed already)
                if ((j < startAreaIndices.y) || (j >= startAreaIndices.y + NumberOfCubesPerArea.y))
                {
                    // Should number remain disabled based on previous backtracking information?
                    if (!ShouldCubeRemainDisabled(CubeControllerMatrix[cubeIndices.x, j], invalidCubeData))
                    {
                        // Only re-enable cube controller if appropriate based on area and column restrictions
                        Vector2Int startAreaIndicesForSelectedCube = CalculateAreaStartIndicesFromCubeIndices(new Vector2Int(cubeIndices.x, j));
                        if ((!DoesNumberExistInArea(cubeIndices.x, j, startAreaIndicesForSelectedCube, number)))
                        {
                            if (!DoesNumberExistInColumn(cubeIndices.x, j, startAreaIndicesForSelectedCube, number))
                            {
                                if (CubeControllerMatrix[cubeIndices.x, j].CubeNumber == 0)
                                    CubeControllerMatrix[cubeIndices.x, j].ToggleSpecificNumberControllerState(number, true);
                                CubeControllerMatrix[cubeIndices.x, j].UpdateAvailableNumbers(number, true, false);
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Helper method for checking whether a cube update matches any of the cube data in the provided list of cube data.
    /// This provided list contains invalid cube information obtained from backtracking.
    /// </summary>
    /// <param name="selectedCubeController">Cube controller that is going to have a number selected.</param>
    /// <param name="invalidCubeData">A list of invalid cube data, which was obtained from backtracking.</param>
    /// <returns>True if the selected cube update is invalid. False otherwise.</returns>
    private bool ShouldCubeRemainDisabled(CubeController selectedCubeController, List<BacktrackCubeData> invalidCubeData)
    {
        // Check if the cube number + cube indices match any cube data in the invalid cube data map
        foreach (BacktrackCubeData cubeData in invalidCubeData)
            if (cubeData.CubeNumber == selectedCubeController.CubeNumber)
                if ((cubeData.CubeIndices.x == selectedCubeController.CubeIndices.x) && (cubeData.CubeIndices.y == selectedCubeController.CubeIndices.y))
                    return true;
        return false;
    }

    /// <summary>
    /// Helper method for identifying if a number exists within a specified area of the sudoku board.
    /// </summary>
    /// <param name="rowIndex">The row index of the re-enabled cube controller.</param>
    /// <param name="colIndex">The column index of the re-enabled cube controller.</param>
    /// <param name="startAreaIndices">The starting indices of the area associated with the re-enabled cube game object.</param>
    /// <param name="number">The number that is checked within the area of the sudoku board.</param>
    /// <returns>True if number exists in area. False if otherwise.</returns>
    private bool DoesNumberExistInArea(int rowIndex, int colIndex, Vector2Int startAreaIndices, int number)
    {
        // Iterate through all cube controllers of the specified area
        for (int x = startAreaIndices.x; x < startAreaIndices.x + NumberOfCubesPerArea.x; x++)
        {
            for (int y = startAreaIndices.y; y < startAreaIndices.y + NumberOfCubesPerArea.y; y++)
            {
                // Ignore the re-enabled cube itself
                if ((x != rowIndex) || (y != colIndex))
                {
                    if (CubeControllerMatrix[x, y].CubeNumber == number)
                        return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Helper method for identifying if a number exists within a specified column of the sudoku board.
    /// </summary>
    /// <param name="rowIndex">The row index of the re-enabled cube controller.</param>
    /// <param name="colIndex">The column index of the re-enabled cube controller.</param>
    /// <param name="startAreaIndices">The starting indices of the area associated with the re-enabled cube game object.</param>
    /// <param name="number">The number that is checked within the rows of the sudoku board.</param>
    /// <returns>True if number exists in the column. False if otherwise.</returns>
    private bool DoesNumberExistInColumn(int rowIndex, int colIndex, Vector2Int startAreaIndices, int number)
    {
        // Iterate through all rows of the sudoku board
        for (int i = 0; i < CubeControllerMatrix.GetLength(0); i++)
        {
            // Ignore the re-enabled cube itself
            if (rowIndex != i)
            {
                // Only consider cubes outside the area (area already checked)
                if ((i < startAreaIndices.x) || (i >= startAreaIndices.x + NumberOfCubesPerArea.x))
                {
                    if (CubeControllerMatrix[i, colIndex].CubeNumber == number)
                        return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Helper method for identifying if a number exists within a specified row of the sudoku board.
    /// </summary>
    /// <param name="rowIndex">The row index of the re-enabled cube controller.</param>
    /// <param name="colIndex">The column index of the re-enabled cube controller.</param>
    /// <param name="startAreaIndices">The starting indices of the area associated with the re-enabled cube game object.</param>
    /// <param name="number">The number that is checked within the columns of the sudoku board.</param>
    /// <returns>True if number exists in the row. False if otherwise.</returns>
    private bool DoesNumberExistInRow(int rowIndex, int colIndex, Vector2Int startAreaIndices, int number)
    {
        // Iterate through all columns of the sudoku board
        for (int j = 0; j < CubeControllerMatrix.GetLength(1); j++)
        {
            // Ignore the re-enabled cube itself
            if (colIndex != j)
            {
                // Only consider cubes outside the area (area already checked)
                if ((j < startAreaIndices.y) || (j >= startAreaIndices.y + NumberOfCubesPerArea.y))
                {
                    if (CubeControllerMatrix[rowIndex, j].CubeNumber == number)
                        return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Callback for adjusting the available count map when a number is selected for a cube
    /// controller.
    /// </summary>
    /// <param name="cubeIndices">Indices of cube that has been selected.</param>
    /// <param name="prevCount">Previous count of available numbers for the selected cube controller.</param>
    /// <param name="newCount">New count of available numbers for the selected cube controller.</param>
    public void UpdateAvailableNumberCountMapSelect(Vector2Int cubeIndices, int prevCount, int newCount)
    {
        // Remove from count map and add to list of cube controllers that are selected
        NumberCountToCubeListMap[prevCount].Remove(CubeControllerMatrix[cubeIndices.x, cubeIndices.y]);
        SelectedCubeList.Add(CubeControllerMatrix[cubeIndices.x, cubeIndices.y]);
    }

    /// <summary>
    /// Callback for adjusting the available count map when changes are propagated to cubes from
    /// enabling/disabling a specific cube controller.
    /// </summary>
    /// <param name="cubeIndices">Indices of cube that has been affected by updates propagated from other cube controllers.</param>
    /// <param name="prevCount">Previous count of available numbers for the affected cube controller.</param>
    /// <param name="newCount">New count of available numbers for the affected cube controller.</param>
    public void UpdateAvailableNumberCountMapPropagate(Vector2Int cubeIndices, int prevCount, int newCount)
    {
        // Move the cube controller from the previous list to the new list
        NumberCountToCubeListMap[prevCount].Remove(CubeControllerMatrix[cubeIndices.x, cubeIndices.y]);
        NumberCountToCubeListMap[newCount].Add(CubeControllerMatrix[cubeIndices.x, cubeIndices.y]);
    }

    /// <summary>
    /// Helper method used during the initialisation of the sudoku board. Places the cube
    /// controller in the correct list of the available count map.
    /// </summary>
    /// <param name="cubeController">The cube controller to be placed in available count map</param>
    private void AddCubeToAvailableCountMap(CubeController cubeController)
    {
        if (cubeController.CubeNumber != 0)
            SelectedCubeList.Add(cubeController);
        else
            NumberCountToCubeListMap[cubeController.CountAvailableNumbersForCube()].Add(cubeController);
    }

    /// <summary>
    /// This method checks if there are any invalid cubes available, where
    /// the cube has no available numbers to be selected and the number has no value.
    /// </summary>
    /// <returns>True if board is valid. False if board invalid.</returns>
    public bool IsBoardValid()
    {
        // If cube controllers exist in this list - cube controllers with no available numbers + no selected number
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
        return SelectedCubeList.Count == (NumberOfAreasInBoard.x * NumberOfCubesPerArea.x * NumberOfAreasInBoard.y * NumberOfCubesPerArea.y);
    }

    /// <summary>
    /// Method for selecting the next cube to be selected.
    /// </summary>
    /// <returns>Cube controller of cube game object to be selected.</returns>
    public CubeController SelectLowestEntropyCube()
    {
        // Iterate through cube controller lists, from lowest to highest available counts - then select random one
        for (int i = 1; i < 10; i++)
            if (NumberCountToCubeListMap[i].Count > 0)
                return NumberCountToCubeListMap[i][Random.Range(0, NumberCountToCubeListMap[i].Count)];
        return null;
    }
}
