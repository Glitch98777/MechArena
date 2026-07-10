using UnityEngine;

// Tiny self-animating effects: a growing/shrinking emissive blob or a quick beam line.
public class Fx : MonoBehaviour
{
    float life, maxLife;
    Vector3 startScale, endScale;
    LineRenderer line;
    float lineStartW;

    public static void Blob(Vector3 pos, Color c, float startR, float endR, float dur)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Fx";
        Destroy(go.GetComponent<Collider>());
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * startR;
        go.GetComponent<MeshRenderer>().sharedMaterial = MechBuilder.EmissiveMat(c);
        var f = go.AddComponent<Fx>();
        f.maxLife = f.life = dur;
        f.startScale = Vector3.one * startR;
        f.endScale = Vector3.one * endR;
    }

    public static void Beam(Vector3 a, Vector3 b, Color c, float dur, float width)
    {
        var go = new GameObject("Beam");
        var lr = go.AddComponent<LineRenderer>();
        lr.material = MechBuilder.EmissiveMat(c);
        lr.startWidth = lr.endWidth = width;
        lr.positionCount = 2;
        lr.SetPosition(0, a); lr.SetPosition(1, b);
        lr.numCapVertices = 4;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var f = go.AddComponent<Fx>();
        f.maxLife = f.life = dur; f.line = lr; f.lineStartW = width;
        f.startScale = f.endScale = Vector3.one;
    }

    void Update()
    {
        life -= Time.deltaTime;
        float t = 1f - Mathf.Clamp01(life / maxLife);
        if (line != null)
        {
            float w = Mathf.Lerp(lineStartW, 0f, t);
            line.startWidth = line.endWidth = w;
        }
        else
        {
            transform.localScale = Vector3.Lerp(startScale, endScale, t);
        }
        if (life <= 0f) Destroy(gameObject);
    }
}
