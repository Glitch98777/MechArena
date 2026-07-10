using System;
using System.Collections;
using UnityEngine;

// A reward crate with a shake + lid-burst open animation.
public class Crate : MonoBehaviour
{
    Transform lidPivot, body;
    Color accent;
    bool opening;

    public static Crate Build(Vector3 pos, Color accent)
    {
        var root = new GameObject("Crate");
        root.transform.position = pos;
        var c = root.AddComponent<Crate>();
        c.accent = accent;

        Material metal = MechBuilder.Mat(new Color(0.28f, 0.3f, 0.36f), 0.8f, 0.5f);
        Material dark = MechBuilder.Mat(new Color(0.12f, 0.13f, 0.16f), 0.7f, 0.4f);
        Material glow = MechBuilder.EmissiveMat(accent);

        float sz = 1.6f;
        c.body = Prim(PrimitiveType.Cube, root.transform, new Vector3(0, sz * 0.4f, 0), new Vector3(sz, sz * 0.8f, sz), metal);
        // edge rails
        for (int xi = -1; xi <= 1; xi += 2)
            for (int zi = -1; zi <= 1; zi += 2)
                Prim(PrimitiveType.Cube, root.transform, new Vector3(xi * sz * 0.48f, sz * 0.4f, zi * sz * 0.48f), new Vector3(0.12f, sz * 0.82f, 0.12f), dark);
        // glowing seam + emblem
        Prim(PrimitiveType.Cube, root.transform, new Vector3(0, sz * 0.4f, sz * 0.51f), new Vector3(sz * 0.7f, 0.1f, 0.05f), glow);
        Prim(PrimitiveType.Cube, root.transform, new Vector3(0, sz * 0.55f, sz * 0.52f), new Vector3(0.16f, 0.36f, 0.05f), glow);
        Prim(PrimitiveType.Cube, root.transform, new Vector3(0, sz * 0.55f, sz * 0.52f), new Vector3(0.36f, 0.16f, 0.05f), glow);

        // lid, hinged at the back-top edge
        c.lidPivot = new GameObject("LidPivot").transform;
        c.lidPivot.SetParent(root.transform, false);
        c.lidPivot.localPosition = new Vector3(0, sz * 0.8f, -sz * 0.5f);
        Prim(PrimitiveType.Cube, c.lidPivot, new Vector3(0, 0.06f, sz * 0.5f), new Vector3(sz, 0.16f, sz), metal);
        Prim(PrimitiveType.Cube, c.lidPivot, new Vector3(0, 0.14f, sz * 0.5f), new Vector3(sz * 0.7f, 0.06f, sz * 0.7f), glow);
        return c;
    }

    static Transform Prim(PrimitiveType t, Transform parent, Vector3 pos, Vector3 scale, Material m)
    {
        var go = GameObject.CreatePrimitive(t);
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = m;
        return go.transform;
    }

    public void Open(Action onReveal)
    {
        if (opening) return;
        opening = true;
        StartCoroutine(OpenRoutine(onReveal));
    }

    IEnumerator OpenRoutine(Action onReveal)
    {
        Vector3 basePos = transform.position;
        // build-up shake
        float t = 0f;
        while (t < 0.9f)
        {
            t += Time.deltaTime;
            float amp = Mathf.Lerp(0.02f, 0.12f, t / 0.9f);
            transform.position = basePos + new Vector3(
                (Mathf.PerlinNoise(t * 40f, 0) - 0.5f) * amp,
                0,
                (Mathf.PerlinNoise(0, t * 40f) - 0.5f) * amp);
            yield return null;
        }
        transform.position = basePos;

        // burst
        Fx.Blob(basePos + Vector3.up * 1.2f, accent, 0.6f, 5f, 0.5f);
        for (int i = 0; i < 22; i++)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(g.GetComponent<Collider>());
            g.transform.position = basePos + Vector3.up * 1.2f;
            g.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.08f, 0.2f);
            g.GetComponent<MeshRenderer>().sharedMaterial = MechBuilder.EmissiveMat(accent, 1.6f);
            var rb = g.AddComponent<Rigidbody>();
            rb.AddForce(Vector3.up * UnityEngine.Random.Range(4f, 8f) + UnityEngine.Random.insideUnitSphere * 5f, ForceMode.Impulse);
            rb.AddTorque(UnityEngine.Random.insideUnitSphere * 10f, ForceMode.Impulse);
            Destroy(g, 2f);
        }
        onReveal?.Invoke();

        // fling lid open
        float a = 0f;
        while (a < 1f)
        {
            a += Time.deltaTime * 3f;
            lidPivot.localRotation = Quaternion.Euler(Mathf.Lerp(0, -115f, Mathf.SmoothStep(0, 1, a)), 0, 0);
            yield return null;
        }
    }
}
