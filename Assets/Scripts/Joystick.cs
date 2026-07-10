using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// On-screen virtual joystick. Value is a Vector2 in [-1,1].
public class Joystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public Vector2 Value { get; private set; }
    RectTransform baseRect, handle;
    float radius = 90f;

    public static Joystick Create(Transform canvas, Vector2 anchoredPos, float size, Color baseCol, Color handleCol)
    {
        var go = new GameObject("Joystick", typeof(RectTransform));
        go.transform.SetParent(canvas, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0, 0);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(size, size);
        rt.anchoredPosition = anchoredPos;

        var img = go.AddComponent<Image>();
        img.color = baseCol;
        img.sprite = UIManager.CircleSprite();
        img.type = Image.Type.Simple;

        var js = go.AddComponent<Joystick>();
        js.baseRect = rt;
        js.radius = size * 0.5f;

        var h = new GameObject("Handle", typeof(RectTransform));
        h.transform.SetParent(go.transform, false);
        var hrt = (RectTransform)h.transform;
        hrt.sizeDelta = new Vector2(size * 0.45f, size * 0.45f);
        hrt.anchoredPosition = Vector2.zero;
        var himg = h.AddComponent<Image>();
        himg.color = handleCol;
        himg.sprite = UIManager.CircleSprite();
        js.handle = hrt;
        return js;
    }

    public void OnPointerDown(PointerEventData e) { OnDrag(e); }

    public void OnDrag(PointerEventData e)
    {
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(baseRect, e.position, e.pressEventCamera, out local);
        Vector2 v = Vector2.ClampMagnitude(local, radius);
        handle.anchoredPosition = v;
        Value = v / radius;
    }

    public void OnPointerUp(PointerEventData e)
    {
        Value = Vector2.zero;
        handle.anchoredPosition = Vector2.zero;
    }
}
