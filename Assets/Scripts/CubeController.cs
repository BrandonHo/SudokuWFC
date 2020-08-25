using UnityEngine;
using UnityEngine.Events;
using TMPro;
using System.Collections.Generic;

public class CubeController : MonoBehaviour
{
    private int CubeNumber;
    private bool[] AvailableNumbersForCube;
    public TextMeshProUGUI MainCubeText;
    public Vector2Int CubeIndices;
    public UnityEventCubeUpdate OnCubeUpdateEvent;

    public NumberController NumberController1, NumberController2, NumberController3,
        NumberController4, NumberController5, NumberController6,
        NumberController7, NumberController8, NumberController9;
    private Dictionary<int, NumberController> NumberToNumberControllerMap;

    public class UnityEventCubeUpdate : UnityEvent<int, Vector2Int>
    {
        // Event for notifying area about cube updates
    }

    void Awake()
    {
        // Initialise event for notifying when cube updates occur (board controller)
        OnCubeUpdateEvent = new UnityEventCubeUpdate();
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
        // Update number + main text to reflect the newly selected number
        CubeNumber = number;
        if (MainCubeText)
        {
            MainCubeText.text = number + "";
            MainCubeText.enabled = true;
        }

        // Disable other numbers from being selected
        DisableNumberControllers();

        // Notify the board controller about cube update
        if (OnCubeUpdateEvent != null)
            OnCubeUpdateEvent.Invoke(CubeNumber, CubeIndices);
    }

    private void DisableNumberControllers()
    {
        foreach (KeyValuePair<int, NumberController> keyValuePair in NumberToNumberControllerMap)
            keyValuePair.Value.ToggleNumberControllerState(false);
    }

    public void DisableNumber(int number)
    {
        AvailableNumbersForCube[number - 1] = false;

        if (NumberToNumberControllerMap.ContainsKey(number))
            NumberToNumberControllerMap[number].ToggleNumberControllerState(false);
    }

    public void RefreshNumberControllerStates()
    {
        for (int i = 0; i < 9; i++)
            NumberToNumberControllerMap[i + 1].ToggleNumberControllerState(AvailableNumbersForCube[i]);
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
}
