using UnityEngine;
using UnityEngine.Events;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// This class describes the functional logic for a cube within a sudoku board.
/// </summary>
public class CubeController : MonoBehaviour
{
    public int CubeNumber;                          // Number associated with the cube controller
    private bool[] AvailableNumbersForCube;         // An array that indicates which numbers can be selected for this cube
    public TextMeshProUGUI MainCubeText;            // The primary text that illustrates the number associated with the cube controller
    public Vector2Int CubeIndices;                  // Indices of the cube in the sudoku board
    private UnityEventCubeUpdate OnCubeUpdateEvent; // Event for notifying listeners when a number is selected for the cube controller

    /*
        These events are used to trigger specific changes to the available count map in the board controller. This count map
        is used to quickly query cube controllers with specific counts of available numbers to be selected.
    */
    public UnityEventAvailableNumbersUpdate OnAvailableNumbersUpdateSelect, OnAvailableNumbersUpdatePropagate;

    public class UnityEventAvailableNumbersUpdate : UnityEvent<Vector2Int, int, int>
    {
        // Custom event for triggering specific changes to the available count map in the board controller.
    }

    /*
        These number controllers are associated with the cube controller, and are interacted with to select a number 
        for the cube controller.
    */
    public NumberController NumberController1, NumberController2, NumberController3,
        NumberController4, NumberController5, NumberController6,
        NumberController7, NumberController8, NumberController9;

    private Dictionary<int, NumberController> NumberToNumberControllerMap;  // Map that is used to quickly access specific number controllers in the cube

    void Awake()
    {
        OnCubeUpdateEvent = new UnityEventCubeUpdate();
        OnAvailableNumbersUpdateSelect = new UnityEventAvailableNumbersUpdate();
        OnAvailableNumbersUpdatePropagate = new UnityEventAvailableNumbersUpdate();
    }

    public void Initialise()
    {
        SetupNumberControllerMap();
        if (MainCubeText)
            MainCubeText.enabled = false;
    }

    private void SetupNumberControllerMap()
    {
        // Add each of the specific number controllers to the map
        NumberToNumberControllerMap = new Dictionary<int, NumberController>();
        NumberToNumberControllerMap.Add(1, NumberController1);
        NumberToNumberControllerMap.Add(2, NumberController2);
        NumberToNumberControllerMap.Add(3, NumberController3);
        NumberToNumberControllerMap.Add(4, NumberController4);
        NumberToNumberControllerMap.Add(5, NumberController5);
        NumberToNumberControllerMap.Add(6, NumberController6);
        NumberToNumberControllerMap.Add(7, NumberController7);
        NumberToNumberControllerMap.Add(8, NumberController8);
        NumberToNumberControllerMap.Add(9, NumberController9);

        // Attach cube controller as listener to the specified number controllers
        foreach (KeyValuePair<int, NumberController> keyValuePair in NumberToNumberControllerMap)
            keyValuePair.Value.AddNumberClickEventListener(OnNumberClickCallback);
    }

    /// <summary>
    /// Primary method for selecting a number for the cube controller.
    /// </summary>
    /// <param name="number">Number to be selected for the cube controller</param>
    public void OnNumberClickCallback(int number)
    {
        // Update main text to reflect the newly selected number
        CubeNumber = number;
        if (MainCubeText)
        {
            MainCubeText.text = number + "";
            MainCubeText.enabled = true;
        }

        // Disable other numbers from being selected
        ToggleStateForNumberControllers(false);

        // Notify the board controller about cube update
        if (OnCubeUpdateEvent != null)
            OnCubeUpdateEvent.Invoke(number, CubeIndices);
    }

    /// <summary>
    /// Method used to revert the state of a selected cube controller
    /// back to a unselected cube controller.
    /// </summary>
    public void ResetCubeController()
    {
        CubeNumber = 0;
        if (MainCubeText)
        {
            MainCubeText.text = CubeNumber + "";
            MainCubeText.enabled = false;
        }

        ResetNumberControllerStates();
    }

    private void ResetNumberControllerStates()
    {
        for (int x = 0; x < AvailableNumbersForCube.Length; x++)
            NumberToNumberControllerMap[x + 1].ToggleNumberControllerState(AvailableNumbersForCube[x]);
    }

    private void ToggleStateForNumberControllers(bool enableNumberControllers)
    {
        foreach (KeyValuePair<int, NumberController> keyValuePair in NumberToNumberControllerMap)
            keyValuePair.Value.ToggleNumberControllerState(enableNumberControllers);
    }

    public void ToggleSpecificNumberControllerState(int number, bool newState)
    {
        if (NumberToNumberControllerMap.ContainsKey(number))
        {
            // Disable the appropriate number controller
            NumberToNumberControllerMap[number].ToggleNumberControllerState(newState);
        }
    }

    public void UpdateAvailableNumbers(int number, bool numberState, bool selectCube)
    {
        // Update available numbers map/array
        int prevCount = CountAvailableNumbersForCube();
        AvailableNumbersForCube[number - 1] = numberState;
        int newCount = CountAvailableNumbersForCube();

        // Only notify board if the available numbers changed
        if (prevCount != newCount)
        {
            // If the cube is newly selected
            if (selectCube)
                OnAvailableNumbersUpdateSelect.Invoke(CubeIndices, prevCount, newCount);

            // Remaining cases - where you simply adjust count map from propagation
            // NB - should only adjust count map if the number is not yet set/selected
            else if (CubeNumber == 0)
                OnAvailableNumbersUpdatePropagate.Invoke(CubeIndices, prevCount, newCount);
        }
    }

    public void SetCubeData(SudokuCubeData cubeData)
    {
        CubeNumber = cubeData.Number;
        AvailableNumbersForCube = cubeData.AvailableNumbers;
        CubeIndices = cubeData.CubeIndices;

        if (CubeNumber != 0)
        {
            MainCubeText.text = CubeNumber + "";
            MainCubeText.enabled = true;
            ToggleStateForNumberControllers(false);
        }
    }

    public void UpdateAvailableNumberState(int number, bool numberState)
    {
        AvailableNumbersForCube[number - 1] = numberState;
    }

    public int CountAvailableNumbersForCube()
    {
        int counter = 0;
        for (int i = 0; i < AvailableNumbersForCube.Length; i++)
            if (AvailableNumbersForCube[i])
                counter++;
        return counter;
    }

    public void AddOnCubeEventListener(UnityAction<int, Vector2Int> onCubeUpdateCallback)
    {
        if (OnCubeUpdateEvent != null)
            OnCubeUpdateEvent.AddListener(onCubeUpdateCallback);
    }

    public void AddOnAvailableNumbersUpdateEventListener(UnityAction<Vector2Int, int, int> onAvailableNumbersUpdateCallback,
        bool select, bool deselect)
    {
        if ((select) && (OnAvailableNumbersUpdateSelect != null))
            OnAvailableNumbersUpdateSelect.AddListener(onAvailableNumbersUpdateCallback);
        else if (OnAvailableNumbersUpdatePropagate != null)
            OnAvailableNumbersUpdatePropagate.AddListener(onAvailableNumbersUpdateCallback);
    }
}
