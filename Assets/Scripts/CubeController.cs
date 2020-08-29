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
    public UnityEventAvailableNumbersUpdate OnAvailableNumbersUpdateEvent;

    public NumberController NumberController1, NumberController2, NumberController3,
        NumberController4, NumberController5, NumberController6,
        NumberController7, NumberController8, NumberController9;
    private Dictionary<int, NumberController> NumberToNumberControllerMap;

    public class UnityEventAvailableNumbersUpdate : UnityEvent<bool, Vector2Int, bool>
    {

    }

    void Awake()
    {
        // Initialise event for notifying when cube updates occur (board controller)
        OnCubeUpdateEvent = new UnityEventCubeUpdate();
        OnAvailableNumbersUpdateEvent = new UnityEventAvailableNumbersUpdate();
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

    private void ToggleStateForNumberControllers(bool enableNumberControllers)
    {
        foreach (KeyValuePair<int, NumberController> keyValuePair in NumberToNumberControllerMap)
            keyValuePair.Value.ToggleNumberControllerState(enableNumberControllers);
    }

    public void RemoveNumberFromAvailableNumbersArray(int number, bool numberState, bool numberSelected)
    {
        bool newChange = AvailableNumbersForCube[number - 1] != numberState;
        AvailableNumbersForCube[number - 1] = numberState;

        if (newChange)
            OnAvailableNumbersUpdateEvent.Invoke(numberState, CubeIndices, numberSelected);
    }

    public void ToggleNumberState(int number, bool disableNumber)
    {
        if (NumberToNumberControllerMap.ContainsKey(number))
        {
            // Disable the appropriate number controller
            NumberToNumberControllerMap[number].ToggleNumberControllerState(disableNumber);
        }
    }

    public void RefreshCubeController()
    {
        if (CubeNumber != 0)
        {
            MainCubeText.text = CubeNumber + "";
            MainCubeText.enabled = true;
        }
        else
        {
            MainCubeText.enabled = false;

            for (int i = 0; i < 9; i++)
                NumberToNumberControllerMap[i + 1].ToggleNumberControllerState(AvailableNumbersForCube[i]);
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

    public void AddOnAvailableNumbersUpdateEventListener(UnityAction<bool, Vector2Int, bool> onAvailableNumbersUpdateCallback)
    {
        if (OnAvailableNumbersUpdateEvent != null)
            OnAvailableNumbersUpdateEvent.AddListener(onAvailableNumbersUpdateCallback);
    }
}
