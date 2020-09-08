using UnityEngine;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// This class describes the functional logic of a controller for a selectable number 
/// within a cube controller.
/// </summary>
public class NumberController : MonoBehaviour
{
    public int Number;                                  // Number associated with the controller
    public TextMeshProUGUI NumberText;                  // Text associated with the controller
    private UnityEventNumberClick OnNumberClickEvent;   // Event that notifies listeners when the number controller is selected

    public class UnityEventNumberClick : UnityEvent<int>
    {
        // Custom event that notifies cube controllers when a number is clicked
    }

    void Awake()
    {
        OnNumberClickEvent = new UnityEventNumberClick();
    }

    void Start()
    {
        if (NumberText)
            NumberText.text = Number + "";
    }

    void OnMouseDown()
    {
        // Mouse click on game object detected - notify appropriate cube controller
        if (OnNumberClickEvent != null)
            OnNumberClickEvent.Invoke(Number);
    }

    public void AddNumberClickEventListener(UnityAction<int> onNumberClickCallback)
    {
        OnNumberClickEvent.AddListener(onNumberClickCallback);
    }

    public void ToggleNumberControllerState(bool newState)
    {
        gameObject.SetActive(newState);
    }


}
