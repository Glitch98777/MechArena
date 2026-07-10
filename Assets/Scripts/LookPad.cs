using UnityEngine;
using UnityEngine.EventSystems;

// Transparent surface on the right of the screen. Dragging it rotates the battle camera.
// It sits BEHIND the fire/ability buttons, so pressing those is never captured as a look-drag.
public class LookPad : MonoBehaviour, IDragHandler, IPointerDownHandler
{
    public void OnPointerDown(PointerEventData e) { }
    public void OnDrag(PointerEventData e)
    {
        if (GameManager.Instance != null) GameManager.Instance.AddLook(e.delta);
    }
}
