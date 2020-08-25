using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class NumberController : MonoBehaviour
{
    public int Number;
    public TextMeshProUGUI NumberText;
    private UnityEventNumberClick OnNumberClickEvent;

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
