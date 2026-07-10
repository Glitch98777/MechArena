using System.Collections.Generic;
using UnityEngine;

// Builds a stylised mech from primitives. Animated joints are unscaled empty pivots;
// scaled visual meshes hang off them as leaves so nothing distorts when they rotate.
public static class MechBuilder
{
    static readonly Dictionary<int, Material> cache = new Dictionary<int, Material>();

    public static Material Mat(Color c, float metallic = 0.35f, float smooth = 0.5f)
    {
        int key = (c.GetHashCode() * 397) ^ Mathf.RoundToInt(metallic * 100) ^ (Mathf.RoundToInt(smooth * 100) << 8);
        if (cache.TryGetValue(key, out var m) && m != null) return m;
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        m = new Material(sh) { color = c };
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        cache[key] = m;
        return m;
    }

    public static Material EmissiveMat(Color c, float intensity = 2.2f)
    {
        int key = (c.GetHashCode() * 131) ^ Mathf.RoundToInt(intensity * 50) ^ 0x5A5A;
        if (cache.TryGetValue(key, out var m) && m != null) return m;
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        m = new Material(sh) { color = c };
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.8f);
        m.EnableKeyword("_EMISSION");
        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", c * intensity);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        cache[key] = m;
        return m;
    }

    // Translucent glowing material for shield bubbles.
    public static Material TransparentMat(Color c, float alpha)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        var m = new Material(sh);
        var col = new Color(c.r, c.g, c.b, alpha);
        m.color = col;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
        m.SetFloat("_Surface", 1f);              // transparent
        m.SetFloat("_Blend", 0f);
        m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.DisableKeyword("_SURFACE_TYPE_OPAQUE");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.EnableKeyword("_EMISSION");
        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", c * 1.5f);
        m.renderQueue = 3000;
        return m;
    }

    static Transform Empty(string name, Transform parent, Vector3 localPos)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.localPosition = localPos;
        return t;
    }

    static Transform Box(Transform parent, Vector3 pos, Vector3 size, Material mat, Vector3? euler = null)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        if (euler.HasValue) go.transform.localEulerAngles = euler.Value;
        go.transform.localScale = size;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go.transform;
    }

    static Transform Cyl(Transform parent, Vector3 pos, Vector3 euler, float radius, float length, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Object.Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localEulerAngles = euler;
        go.transform.localScale = new Vector3(radius * 2f, length * 0.5f, radius * 2f);
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go.transform;
    }

    static Transform Sph(Transform parent, Vector3 pos, float radius, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Object.Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = Vector3.one * (radius * 2f);
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go.transform;
    }

    public static Mech Build(MechData d, WeaponDef weapon, int team, Color? teamTint = null)
    {
        var root = new GameObject("Mech_" + d.name);
        float s = d.heightScale;

        bool legendary  = d.reqRank > 0;
        Material body   = Mat(d.bodyColor, 0.55f, 0.5f);
        Material trim   = legendary ? EmissiveMat(d.accentColor, 2.6f) : Mat(d.accentColor, 0.6f, 0.7f);  // glowing trims for legendaries
        Material dark   = Mat(new Color(0.11f, 0.12f, 0.15f), 0.7f, 0.35f);
        Material steel  = Mat(new Color(0.35f, 0.38f, 0.43f), 0.8f, 0.55f);
        Material glow   = EmissiveMat(teamTint ?? d.accentColor);

        float legLen   = d.legLength * s;
        float thighLen = legLen * 0.52f, shinLen = legLen * 0.48f;
        float hipY     = legLen; // feet rest exactly on y=0
        float legThick = d.legThick * s;
        float hipX     = d.bodyWidth * 0.26f * s;

        // ---------------- LEGS ----------------
        Transform lThigh, lShin, rThigh, rShin;
        BuildLeg(root.transform, -hipX, hipY, thighLen, shinLen, legThick, body, dark, steel, glow, out lThigh, out lShin);
        BuildLeg(root.transform,  hipX, hipY, thighLen, shinLen, legThick, body, dark, steel, glow, out rThigh, out rShin);
        // pelvis block
        Box(root.transform, new Vector3(0, hipY, 0), new Vector3(hipX * 2.1f, 0.4f * s, d.bodyDepth * 0.7f * s), dark);

        // ---------------- TORSO (animated pivot) ----------------
        var torso = Empty("TorsoPivot", root.transform, new Vector3(0, hipY, 0));
        float bh = d.bodyHeight * s, bw = d.bodyWidth * s, bd = d.bodyDepth * s;
        // waist (narrow) then chest (broad) -> tapered silhouette
        Box(torso, new Vector3(0, bh * 0.28f, 0), new Vector3(bw * 0.72f, bh * 0.42f, bd * 0.8f), steel);
        var chest = Box(torso, new Vector3(0, bh * 0.72f, 0), new Vector3(bw, bh * 0.5f, bd), body);
        // angled chest plates
        Box(torso, new Vector3(-bw * 0.34f, bh * 0.78f, bd * 0.42f), new Vector3(bw * 0.4f, bh * 0.4f, 0.16f * s), trim, new Vector3(0, 0, 18));
        Box(torso, new Vector3( bw * 0.34f, bh * 0.78f, bd * 0.42f), new Vector3(bw * 0.4f, bh * 0.4f, 0.16f * s), trim, new Vector3(0, 0, -18));
        // glowing core
        Sph(torso, new Vector3(0, bh * 0.7f, bd * 0.5f), 0.16f * s, glow);
        // collar
        Box(torso, new Vector3(0, bh * 0.98f, 0), new Vector3(bw * 0.55f, 0.2f * s, bd * 0.7f), dark);
        // back thrusters
        Cyl(torso, new Vector3(-bw * 0.28f, bh * 0.72f, -bd * 0.55f), new Vector3(18, 0, 0), 0.13f * s, 0.7f * s, dark);
        Cyl(torso, new Vector3( bw * 0.28f, bh * 0.72f, -bd * 0.55f), new Vector3(18, 0, 0), 0.13f * s, 0.7f * s, dark);
        Sph(torso, new Vector3(-bw * 0.28f, bh * 0.42f, -bd * 0.62f), 0.09f * s, glow);
        Sph(torso, new Vector3( bw * 0.28f, bh * 0.42f, -bd * 0.62f), 0.09f * s, glow);

        // ---------------- HEAD (animated pivot) ----------------
        var head = Empty("HeadPivot", torso, new Vector3(0, bh * 1.02f, 0.05f * s));
        BuildHead(head, s, d.headStyle, steel, glow);

        // ---------------- SHOULDERS + ARMS ----------------
        var muzzles = new List<Transform>();
        float shX = bw * 0.5f + 0.18f * s;
        BuildArm(root.transform, torso, -1, shX, hipY, bh, s, d, weapon, body, dark, steel, trim, glow, muzzles);
        BuildArm(root.transform, torso,  1, shX, hipY, bh, s, d, weapon, body, dark, steel, trim, glow, muzzles);

        if (legendary) AddLegendaryFx(root.transform, torso, s, bh, bw, bd, EmissiveMat(d.accentColor, 3f));

        var mech = root.AddComponent<Mech>();
        mech.Init(d, weapon, team, torso, head, lThigh, lShin, rThigh, rShin,
                  root.transform.Find("ArmPivotL"), root.transform.Find("ArmPivotR"), muzzles.ToArray());
        return mech;
    }

    static void BuildLeg(Transform root, float x, float hipY, float thighLen, float shinLen, float thick,
                         Material body, Material dark, Material steel, Material glow,
                         out Transform thigh, out Transform shin)
    {
        Sph(root, new Vector3(x, hipY, 0), thick * 0.6f, steel); // hip joint
        thigh = Empty(x < 0 ? "ThighL" : "ThighR", root, new Vector3(x, hipY, 0));
        Box(thigh, new Vector3(0, -thighLen * 0.5f, 0), new Vector3(thick, thighLen, thick * 0.95f), body);
        Box(thigh, new Vector3(0, -thighLen * 0.5f, thick * 0.4f), new Vector3(thick * 0.5f, thighLen * 0.7f, 0.08f), glow); // trim strip

        Sph(thigh, new Vector3(0, -thighLen, 0), thick * 0.5f, steel); // knee
        shin = Empty(x < 0 ? "ShinL" : "ShinR", thigh, new Vector3(0, -thighLen, 0));
        Box(shin, new Vector3(0, -shinLen * 0.5f, 0), new Vector3(thick * 0.82f, shinLen, thick * 0.85f), dark);
        // angled armoured foot (bottom sits at y=0)
        Box(shin, new Vector3(0, -shinLen + 0.08f, thick * 0.35f), new Vector3(thick * 1.15f, 0.16f, thick * 1.9f), steel);
        Box(shin, new Vector3(0, -shinLen * 0.55f, thick * 0.48f), new Vector3(thick * 0.7f, shinLen * 0.5f, 0.1f), body, new Vector3(20, 0, 0));
    }

    static void BuildArm(Transform root, Transform torso, int side, float shX, float hipY, float bh, float s,
                         MechData d, WeaponDef weapon, Material body, Material dark, Material steel, Material trim,
                         Material glow, List<Transform> muzzles)
    {
        float sx = shX * side;
        float shoulderY = hipY + bh * 0.82f;
        BuildShoulder(torso, sx, bh, s, d.shoulderStyle, side, body, trim, steel, glow);

        var arm = Empty(side < 0 ? "ArmPivotL" : "ArmPivotR", root, new Vector3(sx, shoulderY, 0));
        Cyl(arm, new Vector3(0, -0.32f * s, 0), Vector3.zero, 0.14f * s, 0.62f * s, steel);   // upper arm
        Sph(arm, new Vector3(0, -0.6f * s, 0), 0.16f * s, dark);                              // elbow
        Box(arm, new Vector3(0, -0.85f * s, 0.03f * s), new Vector3(0.32f * s, 0.5f * s, 0.34f * s), body); // forearm

        // Weapon attachment — right arm is the "main" arm for single-barrel weapons.
        bool twin = weapon.id == Wep.Blaster || weapon.id == Wep.Shotgun;
        bool mainArm = side > 0;
        Vector3 tip = new Vector3(0, -1.05f * s, 0.55f * s);

        if (weapon.beam) // laser lance emitter on main arm
        {
            if (mainArm)
            {
                Box(arm, new Vector3(0, -1.0f * s, 0.35f * s), new Vector3(0.26f * s, 0.26f * s, 0.7f * s), dark);
                Cyl(arm, tip, new Vector3(90, 0, 0), 0.06f * s, 0.5f * s, glow);
                muzzles.Add(MakeMuzzle(arm, tip + new Vector3(0, 0, 0.3f * s)));
            }
            else Box(arm, new Vector3(0, -1.05f * s, 0), new Vector3(0.3f * s, 0.22f * s, 0.3f * s), steel); // fist
        }
        else if (weapon.splashRadius > 0f) // rocket pod
        {
            if (mainArm)
            {
                var pod = Box(arm, new Vector3(0, -0.75f * s, 0.15f * s), new Vector3(0.42f * s, 0.5f * s, 0.5f * s), steel);
                for (int r = 0; r < 4; r++)
                    Sph(pod, new Vector3((r % 2 == 0 ? -0.22f : 0.22f), (r < 2 ? 0.22f : -0.22f), 0.5f), 0.14f, glow);
                muzzles.Add(MakeMuzzle(arm, new Vector3(0, -0.75f * s, 0.6f * s)));
            }
            else Box(arm, new Vector3(0, -1.05f * s, 0), new Vector3(0.3f * s, 0.22f * s, 0.3f * s), steel);
        }
        else if (weapon.projectileSize >= 0.45f) // big single cannon / railgun on main arm
        {
            if (mainArm)
            {
                Cyl(arm, new Vector3(0, -1.0f * s, 0.5f * s), new Vector3(90, 0, 0), 0.18f * s, 1.0f * s, dark);
                Cyl(arm, new Vector3(0, -1.0f * s, 1.0f * s), new Vector3(90, 0, 0), 0.2f * s, 0.2f * s, trim);
                muzzles.Add(MakeMuzzle(arm, new Vector3(0, -1.0f * s, 1.05f * s)));
            }
            else Box(arm, new Vector3(0, -1.05f * s, 0), new Vector3(0.3f * s, 0.22f * s, 0.3f * s), steel);
        }
        else // twin barrels (blaster / scatter) on both arms
        {
            Cyl(arm, new Vector3(0, -1.0f * s, 0.5f * s), new Vector3(90, 0, 0), 0.09f * s, 0.7f * s, trim);
            Cyl(arm, new Vector3(0, -0.9f * s, 0.45f * s), new Vector3(90, 0, 0), 0.09f * s, 0.55f * s, dark);
            muzzles.Add(MakeMuzzle(arm, new Vector3(0, -1.0f * s, 0.9f * s)));
            if (!twin) { } // (kept simple: both arms carry a muzzle)
        }
    }

    static void BuildHead(Transform head, float s, int style, Material steel, Material glow)
    {
        if (style == 1) // scout: round head + single eye
        {
            Sph(head, new Vector3(0, 0.18f * s, 0), 0.22f * s, steel);
            Box(head, new Vector3(0, 0.18f * s, 0.2f * s), new Vector3(0.16f * s, 0.16f * s, 0.06f * s), glow);
            Cyl(head, new Vector3(0.12f * s, 0.44f * s, -0.05f * s), new Vector3(-18, 0, 10), 0.02f * s, 0.5f * s, steel);
            Sph(head, new Vector3(0.17f * s, 0.68f * s, -0.1f * s), 0.03f * s, glow);
        }
        else if (style == 2) // wide, horned
        {
            Box(head, new Vector3(0, 0.16f * s, 0), new Vector3(0.56f * s, 0.32f * s, 0.46f * s), steel);
            Box(head, new Vector3(0, 0.16f * s, 0.24f * s), new Vector3(0.62f * s, 0.1f * s, 0.06f * s), glow);
            Box(head, new Vector3(-0.26f * s, 0.4f * s, 0), new Vector3(0.08f * s, 0.28f * s, 0.08f * s), steel, new Vector3(0, 0, 28));
            Box(head, new Vector3(0.26f * s, 0.4f * s, 0), new Vector3(0.08f * s, 0.28f * s, 0.08f * s), steel, new Vector3(0, 0, -28));
        }
        else // 0: box + visor slit + antenna
        {
            Box(head, new Vector3(0, 0.16f * s, 0), new Vector3(0.42f * s, 0.34f * s, 0.44f * s), steel);
            Box(head, new Vector3(0, 0.16f * s, 0.22f * s), new Vector3(0.5f * s, 0.13f * s, 0.06f * s), glow);
            Cyl(head, new Vector3(0.16f * s, 0.42f * s, 0), new Vector3(0, 0, 6), 0.02f * s, 0.4f * s, steel);
            Sph(head, new Vector3(0.16f * s, 0.62f * s, 0), 0.03f * s, glow);
        }
    }

    static void BuildShoulder(Transform torso, float sx, float bh, float s, int style, int side,
                              Material body, Material trim, Material steel, Material glow)
    {
        Sph(torso, new Vector3(sx, bh * 0.7f, 0), 0.2f * s, steel);
        if (style == 1) // slim
        {
            Box(torso, new Vector3(sx, bh * 0.82f, 0), new Vector3(0.34f * s, 0.3f * s, 0.5f * s), body);
            Box(torso, new Vector3(sx, bh * 0.88f, 0), new Vector3(0.36f * s, 0.08f * s, 0.4f * s), trim);
        }
        else if (style == 2) // spiked
        {
            Box(torso, new Vector3(sx, bh * 0.84f, 0), new Vector3(0.52f * s, 0.44f * s, 0.62f * s), body, new Vector3(0, 0, -14 * side));
            Cyl(torso, new Vector3(sx * 1.05f, bh * 1.02f, 0), new Vector3(0, 0, -30 * side), 0.06f * s, 0.5f * s, trim);
            Sph(torso, new Vector3(sx, bh * 0.9f, 0.3f * s), 0.06f * s, glow);
        }
        else // 0: pauldron
        {
            Box(torso, new Vector3(sx, bh * 0.82f, 0), new Vector3(0.5f * s, 0.42f * s, 0.6f * s), body, new Vector3(0, 0, -12 * side));
            Box(torso, new Vector3(sx * 1.02f, bh * 0.9f, 0), new Vector3(0.3f * s, 0.14f * s, 0.5f * s), trim);
        }
    }

    // Extra glowing flourishes for legendary chassis: base halo ring, chest strips, and back wing crests.
    static void AddLegendaryFx(Transform root, Transform torso, float s, float bh, float bw, float bd, Material glow)
    {
        // glowing halo of nodes at the feet
        int n = 12;
        float r = bw * 0.9f + 0.6f;
        for (int i = 0; i < n; i++)
        {
            float a = i / (float)n * Mathf.PI * 2f;
            Box(root, new Vector3(Mathf.Cos(a) * r, 0.12f, Mathf.Sin(a) * r), new Vector3(0.13f, 0.26f, 0.13f), glow);
        }
        // chest edge strips
        Box(torso, new Vector3(0, bh * 0.7f, bd * 0.52f), new Vector3(bw * 0.85f, 0.06f * s, 0.05f), glow);
        Box(torso, new Vector3(-bw * 0.4f, bh * 0.72f, bd * 0.42f), new Vector3(0.06f * s, bh * 0.42f, 0.05f), glow, new Vector3(0, 0, 12));
        Box(torso, new Vector3(bw * 0.4f, bh * 0.72f, bd * 0.42f), new Vector3(0.06f * s, bh * 0.42f, 0.05f), glow, new Vector3(0, 0, -12));
        // back wing crests
        Box(torso, new Vector3(-bw * 0.32f, bh * 0.98f, -bd * 0.5f), new Vector3(0.5f * s, 0.5f * s, 0.05f), glow, new Vector3(0, 0, 34));
        Box(torso, new Vector3(bw * 0.32f, bh * 0.98f, -bd * 0.5f), new Vector3(0.5f * s, 0.5f * s, 0.05f), glow, new Vector3(0, 0, -34));
    }

    static Transform MakeMuzzle(Transform parent, Vector3 pos)
    {
        var mz = new GameObject("Muzzle").transform;
        mz.SetParent(parent, false);
        mz.localPosition = pos;
        return mz;
    }
}
