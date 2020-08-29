using UnityEngine;
using UnityEngine.Events;

public class UnityEventCubeUpdate : UnityEvent<int, Vector2Int>
{
    // Event for notifying board controller (and WFC manager) about cube updates
}
