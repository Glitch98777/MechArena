using UnityEngine;

// Billboarded health bar. IMPORTANT: it moves its own holder object, never the mech's transform.
public class HealthBar : MonoBehaviour
{
    Transform anchor;      // the mech we float above (read-only)
    Transform holder;      // our own object that carries the bar quads
    float height;
    Transform fill;
    const float Width = 1.6f;

    public void Setup(Transform anchor, float height, Color color)
    {
        this.anchor = anchor; this.height = height;

        holder = new GameObject("HealthBar").transform;   // separate object, NOT parented to the mech
        Quad("HB_BG", new Color(0.05f, 0.05f, 0.05f, 1f), Width, 0.22f, 0f);
        fill = Quad("HB_Fill", color, Width, 0.18f, -0.01f);
    }

    Transform Quad(string name, Color c, float w, float h, float z)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = name;
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(holder, false);
        go.transform.localPosition = new Vector3(0, 0, z);
        go.transform.localScale = new Vector3(w, h, 1f);
        go.GetComponent<MeshRenderer>().sharedMaterial = MechBuilder.EmissiveMat(c, 1.4f);
        return go.transform;
    }

    public void SetFill(float amt)
    {
        amt = Mathf.Clamp01(amt);
        if (fill != null)
        {
            fill.localScale = new Vector3(Width * amt, 0.18f, 1f);
            fill.localPosition = new Vector3(-Width * (1f - amt) * 0.5f, 0, -0.01f);
        }
    }

    public void Hide() { if (holder != null) holder.gameObject.SetActive(false); }

    void LateUpdate()
    {
        if (anchor == null || holder == null) return;
        holder.position = anchor.position + Vector3.up * height;   // move the HOLDER, not the mech
        var cam = Camera.main;
        if (cam != null)
            holder.rotation = Quaternion.LookRotation(holder.position - cam.transform.position, Vector3.up);
    }

    void OnDestroy() { if (holder != null) Destroy(holder.gameObject); }
}
