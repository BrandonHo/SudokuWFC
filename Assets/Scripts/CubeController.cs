using UnityEngine;
using UnityEngine.Events;
using TMPro;
using System.Collections.Generic;

public class CubeController : MonoBehaviour
{
    public int CubeNumber;
    private bool[] AvailableNumbersForCube;
    public TextMeshProUGUI MainCubeText;
    public Vector2Int CubeIndices;

    public UnityEventCubeUpdate OnCubeUpdateEvent;

    public UnityEventAvailableNumbersUpdate OnAvailableNumbersUpdateSelect,
        OnAvailableNumbersUpdateDeselect,
        OnAvailableNumbersUpdatePropagate;

    public NumberController NumberController1, NumberController2, NumberController3,
        NumberController4, NumberController5, NumberController6,
        NumberController7, NumberController8, NumberController9;
    private Dictionary<int, NumberController> NumberToNumberControllerMap;

    public class UnityEventAvailableNumbersUpdate : UnityEvent<Vector2Int, int, int>
    {

    }

    void Awake()
    {
        // Initialise event for notifying when cube updates occur (board controller)
        OnCubeUpdateEvent = new UnityEventCubeUpdate();

        OnAvailableNumbersUpdateSelect = new UnityEventAvailableNumbersUpdate();
        OnAvailableNumbersUpdateDeselect = new UnityEventAvailableNumbersUpdate();
        OnAvailableNumbersUpdatePropagate = new UnityEventAvailableNumbersUpdate();
    }

    void Start()
    {
        SetupNumberControllerMap();
        if (MainCubeText)
            MainCubeText.enabled = false;
    }

    private void SetupNumberControllerMap()
    {
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

        foreach (KeyValuePair<int, NumberController> keyValuePair in NumberToNumberControllerMap)
            keyValuePair.Value.AddNumberClickEventListener(OnNumberClickCallback);
    }

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

    public void UpdateAvailableNumbers(int number, bool numberState, bool selectCube, bool deselectCube)
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

            // If the cube was previously selected
            else if (deselectCube)
                OnAvailableNumbersUpdateDeselect.Invoke(CubeIndices, prevCount, newCount);

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
        else if ((deselect) && (OnAvailableNumbersUpdateDeselect != null))
            OnAvailableNumbersUpdateDeselect.AddListener(onAvailableNumbersUpdateCallback);
        else if (OnAvailableNumbersUpdatePropagate != null)
            OnAvailableNumbersUpdatePropagate.AddListener(onAvailableNumbersUpdateCallback);
    }
}
