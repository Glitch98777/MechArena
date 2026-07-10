using UnityEngine;

// Procedurally-generated sound effects + ambience (no audio assets needed).
public class SfxManager : MonoBehaviour
{
    public static SfxManager I { get; private set; }
    const int SR = 44100;

    AudioSource src, music;
    AudioClip pew, boom, zap, whoosh, scatter, hit, explo, click, ability, shield, sw, crate, rankup, win, lose, ambience;

    void Awake()
    {
        if (I != null && I != this) { Destroy(this); return; }
        I = this;

        src = gameObject.AddComponent<AudioSource>(); src.playOnAwake = false; src.spatialBlend = 0f;
        music = gameObject.AddComponent<AudioSource>(); music.playOnAwake = false; music.spatialBlend = 0f; music.loop = true; music.volume = 0.3f;

        // --- build all clips ---
        pew     = Mk("pew", Mix(Sweep(1000, 260, 0.14f, 24f, 0.5f), Noise(0.05f, 40f, 0.15f, 0.5f)));
        boom    = Mk("boom", Mix(Noise(0.3f, 13f, 0.6f, 0.12f), Tone(72f, 0.35f, 9f, 0.7f, true)));
        zap     = Mk("zap", Mix(Sweep(1700, 1150, 0.12f, 16f, 0.35f), Tone(2300f, 0.1f, 30f, 0.12f, false)));
        whoosh  = Mk("whoosh", Mix(Noise(0.35f, 6f, 0.4f, 0.04f), Tone(130f, 0.3f, 7f, 0.5f, true)));
        scatter = Mk("scatter", Noise(0.18f, 20f, 0.55f, 0.28f));
        hit     = Mk("hit", Mix(Noise(0.06f, 55f, 0.5f, 0.4f), Tone(420f, 0.05f, 45f, 0.2f, false)));
        explo   = Mk("explo", Mix(Noise(0.7f, 5.5f, 0.7f, 0.09f), Tone(56f, 0.6f, 4.5f, 0.7f, true), Sweep(320, 60, 0.5f, 6f, 0.3f)));
        click   = Mk("click", Tone(720f, 0.05f, 38f, 0.22f, true));
        ability = Mk("ability", Mix(Sweep(300, 1300, 0.3f, 6f, 0.3f), Tone(660f, 0.25f, 7f, 0.14f, true)));
        shield  = Mk("shield", Mix(Tone(392f, 0.45f, 3f, 0.2f, true), Tone(523f, 0.45f, 3f, 0.16f, true), Tone(659f, 0.45f, 3f, 0.12f, true)));
        sw      = Mk("switch", Sweep(520, 940, 0.1f, 16f, 0.3f));
        crate   = Mk("crate", Seq(Tone(880, 0.09f, 18f, 0.25f, true), Tone(1175, 0.09f, 18f, 0.25f, true), Tone(1568, 0.09f, 18f, 0.25f, true), Mix(Tone(2093, 0.25f, 9f, 0.3f, true), Noise(0.25f, 8f, 0.15f, 0.5f))));
        rankup  = Mk("rankup", Seq(Tone(523, 0.12f, 9f, 0.32f, true), Tone(659, 0.12f, 9f, 0.32f, true), Tone(784, 0.12f, 9f, 0.32f, true), Tone(1047, 0.35f, 6f, 0.4f, true)));
        win     = Mk("win", Seq(Tone(523, 0.12f, 8f, 0.3f, true), Tone(659, 0.12f, 8f, 0.3f, true), Tone(784, 0.12f, 8f, 0.3f, true), Tone(1047, 0.4f, 5f, 0.38f, true)));
        lose    = Mk("lose", Seq(Tone(440, 0.18f, 6f, 0.3f, true), Tone(370, 0.18f, 6f, 0.3f, true), Tone(294, 0.45f, 5f, 0.33f, true)));
        ambience = Mk("amb", Ambience());

        music.clip = ambience; music.Play();
    }

    // ---------------- public play API ----------------
    public void Click() => Play(click, 0.5f);
    public void Hit(float v = 0.35f) => Play(hit, v);
    public void Explosion() => Play(explo, 0.6f);
    public void Switch() => Play(sw, 0.6f);
    public void Crate() => Play(crate, 0.6f);
    public void RankUp() => Play(rankup, 0.6f);
    public void Win() => Play(win, 0.6f);
    public void Lose() => Play(lose, 0.6f);

    public void Shoot(WeaponDef w, float vol)
    {
        AudioClip c = pew;
        if (w != null)
        {
            if (w.beam) c = zap;
            else if (w.splashRadius > 0f) c = whoosh;
            else if (w.projectileSize >= 0.45f) c = boom;
            else if (w.pellets > 1) c = scatter;
        }
        Play(c, vol);
    }

    public void Ability(AbilityType a, float vol)
    {
        Play(a == AbilityType.Shield ? shield : ability, vol);
    }

    void Play(AudioClip c, float vol) { if (c != null && src != null) src.PlayOneShot(c, Mathf.Clamp01(vol)); }

    // ---------------- generation helpers ----------------
    static AudioClip Mk(string name, float[] data)
    {
        var c = AudioClip.Create(name, data.Length, 1, SR, false);
        c.SetData(data, 0);
        return c;
    }

    static float[] Tone(float freq, float dur, float decay, float amp, bool harmonic)
    {
        int n = (int)(dur * SR); var b = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SR, e = Mathf.Exp(-t * decay);
            float s = Mathf.Sin(2f * Mathf.PI * freq * t);
            if (harmonic) s += 0.35f * Mathf.Sin(2f * Mathf.PI * freq * 2f * t);
            b[i] = s * e * amp;
        }
        return b;
    }

    static float[] Sweep(float f0, float f1, float dur, float decay, float amp)
    {
        int n = (int)(dur * SR); var b = new float[n]; float phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SR, f = Mathf.Lerp(f0, f1, t / dur);
            phase += 2f * Mathf.PI * f / SR;
            b[i] = Mathf.Sin(phase) * Mathf.Exp(-t * decay) * amp;
        }
        return b;
    }

    static float[] Noise(float dur, float decay, float amp, float lp)
    {
        int n = (int)(dur * SR); var b = new float[n]; float prev = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SR;
            float nz = Random.value * 2f - 1f;
            prev = Mathf.Lerp(prev, nz, lp);
            b[i] = prev * Mathf.Exp(-t * decay) * amp;
        }
        return b;
    }

    static float[] Mix(params float[][] parts)
    {
        int len = 0; foreach (var p in parts) len = Mathf.Max(len, p.Length);
        var b = new float[len];
        foreach (var p in parts) for (int i = 0; i < p.Length; i++) b[i] += p[i];
        for (int i = 0; i < len; i++) b[i] = Mathf.Clamp(b[i], -1f, 1f);
        return b;
    }

    static float[] Seq(params float[][] parts)
    {
        int len = 0; foreach (var p in parts) len += p.Length;
        var b = new float[len]; int o = 0;
        foreach (var p in parts) { System.Array.Copy(p, 0, b, o, p.Length); o += p.Length; }
        return b;
    }

    static float[] Ambience()
    {
        int n = 6 * SR; var b = new float[n];
        float[] freqs = { 110f, 165f, 220f };   // integer cycles over 6s -> seamless loop
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SR;
            float lfo = 0.7f + 0.3f * Mathf.Sin(2f * Mathf.PI * (1f / 6f) * t);
            float s = 0f; foreach (var f in freqs) s += Mathf.Sin(2f * Mathf.PI * f * t);
            b[i] = (s / freqs.Length) * 0.5f * lfo;
        }
        return b;
    }
}
