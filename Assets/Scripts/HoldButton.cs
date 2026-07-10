using System;
using UnityEngine;
using UnityEngine.EventSystems;

// A button that reports press-and-hold state (for the fire control).
public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public Action<bool> OnHold;
    public void OnPointerDown(PointerEventData e) { OnHold?.Invoke(true); }
    public void OnPointerUp(PointerEventData e) { OnHold?.Invoke(false); }
    void OnDisable() { OnHold?.Invoke(false); }
}
