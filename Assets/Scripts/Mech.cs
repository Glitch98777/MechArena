using System;
using UnityEngine;

// Runtime mech: procedural walk/idle animation, movement, weapon firing, health.
public class Mech : MonoBehaviour
{
    public MechData Data { get; private set; }
    public WeaponDef Weapon { get; private set; }
    public int Team { get; private set; }
    public float Health { get; private set; }
    public bool Alive { get; private set; } = true;
    public float DamageMultiplier = 1f;

    float maxHpMult = 1f;
    public float MaxHealth => Data.maxHealth * maxHpMult;
    public void SetHealthScale(float m) { maxHpMult = Mathf.Max(0.1f, m); Health = MaxHealth; if (bar != null) bar.SetFill(1f); }

    public event Action<Mech> OnDeath;

    Transform torso, head, lThigh, lShin, rThigh, rShin, armL, armR;
    Transform[] muzzles;
    HealthBar bar;

    Vector3 moveInput;
    bool hasFace;
    Vector3 facePoint;

    float animSpeed, walkPhase, torsoBaseY, fireCooldown;
    int shotIndex;

    // ability state
    AbilityType ability;
    AbilityDef abDef;
    float abilityCd, shieldTimer, dashTimer, overdriveTimer, cloakTimer, stunTimer;
    Vector3 dashDir;
    GameObject shieldObj;
    public bool Shielded => shieldTimer > 0f;
    public float ShieldRemaining => shieldTimer;
    public bool Cloaked => cloakTimer > 0f;
    public bool Stunned => stunTimer > 0f;
    public AbilityDef Ability => abDef;
    public bool AbilityReady => Alive && abilityCd <= 0f && !Stunned;
    public float AbilityCharge => (abDef == null || abDef.cooldown <= 0f) ? 1f : 1f - Mathf.Clamp01(abilityCd / abDef.cooldown);

    public void SetAbility(AbilityType t) { ability = t; abDef = AbilityDef.Get(t); abilityCd = 0f; }
    public void Stun(float t) { if (!Shielded) stunTimer = Mathf.Max(stunTimer, t); }

    public void Init(MechData d, WeaponDef weapon, int team, Transform torso, Transform head,
                     Transform lThigh, Transform lShin, Transform rThigh, Transform rShin,
                     Transform armL, Transform armR, Transform[] muzzles)
    {
        Data = d; Weapon = weapon; Team = team; Health = d.maxHealth;
        ability = d.ability; abDef = AbilityDef.Get(ability);
        this.torso = torso; this.head = head;
        this.lThigh = lThigh; this.lShin = lShin; this.rThigh = rThigh; this.rShin = rShin;
        this.armL = armL; this.armR = armR; this.muzzles = muzzles;
        torsoBaseY = torso.localPosition.y;
    }

    public void EnableHealthBar()
    {
        if (bar != null) return;
        bar = gameObject.AddComponent<HealthBar>();
        float top = (Data.legLength + Data.bodyHeight) * Data.heightScale + 0.9f;
        bar.Setup(transform, top, Team == 0 ? new Color(0.3f, 0.9f, 0.45f) : new Color(0.95f, 0.35f, 0.32f));
    }

    public void SetHealthFraction(float frac)
    {
        Health = Mathf.Max(1f, Mathf.Clamp01(frac) * MaxHealth);
        if (bar != null) bar.SetFill(Health / MaxHealth);
    }

    public void SetMove(Vector3 worldMove) { moveInput = Vector3.ClampMagnitude(worldMove, 1f); }
    public void SetFace(Vector3 worldPoint) { hasFace = true; facePoint = worldPoint; }

    public bool TryFire(Vector3 targetPoint)
    {
        if (!Alive || Stunned || fireCooldown > 0f || muzzles == null || muzzles.Length == 0) return false;
        float rate = Weapon.fireRate * (overdriveTimer > 0f ? 2f : 1f);
        fireCooldown = 1f / Mathf.Max(0.1f, rate);
        float dmg = Weapon.damage * DamageMultiplier;
        SfxManager.I?.Shoot(Weapon, Team == 0 ? 0.5f : 0.16f);

        if (Weapon.beam)
        {
            var mz = muzzles[0];
            var enemy = GameManager.Instance != null ? GameManager.Instance.NearestEnemy(this) : null;
            Vector3 end = mz.position + (targetPoint - mz.position).normalized * Weapon.range;
            if (enemy != null)
            {
                float dist = Vector3.Distance(transform.position, enemy.transform.position);
                if (dist <= Weapon.range)
                {
                    end = enemy.transform.position + Vector3.up * 1.4f;
                    enemy.TakeDamage(dmg);
                }
            }
            Fx.Beam(mz.position, end, Weapon.color, 0.09f, 0.16f);
            Fx.Blob(mz.position, Weapon.color, 0.15f, 0.5f, 0.06f);
            return true;
        }

        if (Weapon.pellets > 1) // spread from one muzzle
        {
            var mz = muzzles[0];
            Vector3 baseDir = (targetPoint - mz.position).normalized;
            for (int i = 0; i < Weapon.pellets; i++)
            {
                float ang = ((i / (float)(Weapon.pellets - 1)) - 0.5f) * Weapon.spreadDeg;
                Vector3 dir = Quaternion.AngleAxis(ang, Vector3.up) * baseDir;
                Projectile.Spawn(mz.position, dir, Weapon.projectileSpeed, dmg, Weapon.projectileSize, 0f, Team, Weapon.color);
            }
            Fx.Blob(mz.position, Weapon.color, 0.2f, 0.7f, 0.06f);
            return true;
        }

        // single projectile (alternate barrels for a nicer look)
        var muzzle = muzzles[shotIndex % muzzles.Length];
        shotIndex++;
        Vector3 d2 = (targetPoint - muzzle.position).normalized;
        Projectile.Spawn(muzzle.position, d2, Weapon.projectileSpeed, dmg, Weapon.projectileSize, Weapon.splashRadius, Team, Weapon.color);
        Fx.Blob(muzzle.position, Weapon.color, 0.15f, 0.55f, 0.06f);
        return true;
    }

    bool teslaResistUsed;
    // Tesla insta-kill entry point: tough mechs survive the first bolt.
    public void ApplyTesla()
    {
        if (!Alive) return;
        if (Data.teslaResistDrop > 0f && !teslaResistUsed)
        {
            teslaResistUsed = true;
            Health = Mathf.Max(1f, Mathf.Min(Health, MaxHealth * Data.teslaResistDrop));   // survives the first bolt
            if (bar != null) bar.SetFill(Health / MaxHealth);
            Fx.Blob(transform.position + Vector3.up * 1.4f, new Color(0.6f, 0.9f, 1f), 0.4f, 1.7f, 0.2f);
            return;
        }
        TakeDamage(999999f);
    }

    public void TakeDamage(float dmg)
    {
        if (!Alive) return;
        if (Shielded) { Fx.Blob(transform.position + Vector3.up * 1.4f, new Color(0.4f, 0.8f, 1f), 0.4f, 1.2f, 0.12f); return; }
        Health = Mathf.Max(0f, Health - dmg);
        if (bar != null) bar.SetFill(Health / MaxHealth);
        if (Team == 0) SfxManager.I?.Hit(0.4f);   // "you're being hit" feedback
        if (Health <= 0f) Die();
    }

    public void ActivateAbility()
    {
        if (!AbilityReady) return;
        abilityCd = abDef.cooldown;
        SfxManager.I?.Ability(ability, Team == 0 ? 0.5f : 0.25f);
        var gm = GameManager.Instance;
        Vector3 core = transform.position + Vector3.up * 1.4f;
        Color acol = abDef.color;

        switch (ability)
        {
            case AbilityType.Shield:
                shieldTimer = 15f; SpawnShield(); if (gm) gm.ShakeCam(0.25f);
                break;

            case AbilityType.Dash:
                dashTimer = 0.35f; dashDir = transform.forward;
                Fx.Blob(core, acol, 0.4f, 1.6f, 0.2f);
                break;

            case AbilityType.Heal:
                Health = Mathf.Min(MaxHealth, Health + MaxHealth * 0.55f);
                if (bar != null) bar.SetFill(Health / MaxHealth);
                Fx.Blob(core, new Color(0.4f, 1f, 0.5f), 0.5f, 2.6f, 0.35f);
                break;

            case AbilityType.Overdrive:
                overdriveTimer = 6f;
                Fx.Blob(core, acol, 0.5f, 2.2f, 0.3f); if (gm) gm.ShakeCam(0.2f);
                break;

            case AbilityType.Cloak:
                cloakTimer = 5f;
                Fx.Blob(core, acol, 0.5f, 2.4f, 0.3f);
                break;

            case AbilityType.EMP:
            {
                Fx.Blob(core, acol, 1f, 12f, 0.45f);
                if (gm != null)
                    foreach (var mm in gm.Mechs)
                        if (mm != null && mm.Alive && mm.Team != Team &&
                            Vector3.Distance(mm.transform.position, transform.position) < 12f)
                            mm.Stun(3f);
                if (gm) gm.ShakeCam(0.4f);
                break;
            }

            case AbilityType.Barrage:
            {
                var e = gm != null ? gm.NearestEnemy(this) : null;
                if (e != null)
                {
                    Vector3 c = e.transform.position;
                    for (int k = 0; k < 6; k++)
                        Fx.Blob(c + new Vector3(Mathf.Cos(k) * 2f, 0.4f, Mathf.Sin(k * 1.7f) * 2f), acol, 0.6f, 3.5f, 0.35f);
                    foreach (var mm in gm.Mechs)
                        if (mm != null && mm.Alive && mm.Team != Team &&
                            Vector3.Distance(mm.transform.position, c) < 7f)
                            mm.TakeDamage(50f);
                    if (gm) gm.ShakeCam(0.5f);
                }
                break;
            }

            case AbilityType.Tesla:
            {
                var e = gm != null ? gm.NearestEnemy(this) : null;
                if (e != null)
                {
                    Fx.Beam(core, e.transform.position + Vector3.up * 1.4f, new Color(0.6f, 0.9f, 1f), 0.3f, 0.4f);
                    Fx.Blob(e.transform.position + Vector3.up * 1.4f, new Color(0.7f, 0.9f, 1f), 0.6f, 3.2f, 0.4f);
                    e.ApplyTesla();
                    if (gm) gm.ShakeCam(0.5f);
                }
                break;
            }

            case AbilityType.Slam:
            {
                Fx.Blob(transform.position + Vector3.up * 0.4f, Data.accentColor, 1f, 9f, 0.4f);
                if (gm != null)
                    foreach (var m in gm.Mechs)
                        if (m != null && m.Alive && m.Team != Team &&
                            Vector3.Distance(m.transform.position, transform.position) < 8f)
                            m.TakeDamage(65f);
                if (gm) gm.ShakeCam(0.6f);
                break;
            }

            case AbilityType.PhaseStrike:
            {
                var e = gm != null ? gm.NearestEnemy(this) : null;
                if (e != null)
                {
                    Fx.Blob(core, Data.accentColor, 0.4f, 1.5f, 0.2f);
                    Vector3 behind = e.transform.position - e.transform.forward * 2.5f;
                    transform.position = new Vector3(behind.x, 0f, behind.z);
                    transform.rotation = Quaternion.LookRotation((e.transform.position - transform.position).normalized, Vector3.up);
                    Fx.Blob(e.transform.position + Vector3.up * 1.4f, Data.accentColor, 0.5f, 2.5f, 0.3f);
                    e.TakeDamage(85f);
                    if (gm) gm.ShakeCam(0.4f);
                }
                break;
            }
        }
    }

    void SpawnShield()
    {
        if (shieldObj != null) Destroy(shieldObj);
        shieldObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(shieldObj.GetComponent<Collider>());
        shieldObj.transform.SetParent(transform, false);
        shieldObj.transform.localPosition = new Vector3(0, 1.4f, 0);
        float sc = 3.2f * Data.heightScale;
        shieldObj.transform.localScale = new Vector3(sc, sc * 1.25f, sc);
        shieldObj.GetComponent<MeshRenderer>().sharedMaterial = MechBuilder.TransparentMat(new Color(0.3f, 0.7f, 1f), 0.22f);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (fireCooldown > 0f) fireCooldown -= dt;
        if (abilityCd > 0f) abilityCd -= dt;
        if (overdriveTimer > 0f) overdriveTimer -= dt;
        if (cloakTimer > 0f) cloakTimer -= dt;
        if (stunTimer > 0f) stunTimer -= dt;
        if (shieldTimer > 0f)
        {
            shieldTimer -= dt;
            if (shieldTimer <= 0f && shieldObj != null) { Destroy(shieldObj); shieldObj = null; }
        }

        if (Alive)
        {
            if (dashTimer > 0f) DoDash(dt);
            else Locomote(dt);
        }
        Animate(dt);
    }

    void DoDash(float dt)
    {
        dashTimer -= dt;
        float prog = Mathf.Clamp01(1f - dashTimer / 0.35f);
        Vector3 flat = transform.position + dashDir * 32f * dt;
        float r = GameManager.Instance != null ? GameManager.Instance.ArenaRadius : 40f;
        Vector2 f2 = new Vector2(flat.x, flat.z);
        if (f2.magnitude > r) f2 = f2.normalized * r;
        float y = dashTimer > 0f ? Mathf.Sin(prog * Mathf.PI) * 1.6f : 0f;
        transform.position = new Vector3(f2.x, y, f2.y);
    }

    void Locomote(float dt)
    {
        if (Stunned) return;
        float mag = moveInput.magnitude;
        float spd = Data.moveSpeed * (overdriveTimer > 0f ? 1.5f : 1f);
        if (mag > 0.01f)
        {
            Vector3 pos = transform.position + moveInput * spd * dt;
            float r = GameManager.Instance != null ? GameManager.Instance.ArenaRadius : 40f;
            Vector2 flat = new Vector2(pos.x, pos.z);
            if (flat.magnitude > r) flat = flat.normalized * r;
            transform.position = new Vector3(flat.x, 0f, flat.y);
        }

        Vector3 look = Vector3.zero;
        if (hasFace) look = facePoint - transform.position;
        else if (mag > 0.05f) look = moveInput;
        look.y = 0;
        if (look.sqrMagnitude > 0.001f)
        {
            Quaternion want = Quaternion.LookRotation(look.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, want, Data.turnSpeed * dt);
        }
        hasFace = false;
    }

    void Animate(float dt)
    {
        float target = Alive ? Mathf.Clamp01(moveInput.magnitude) : 0f;
        animSpeed = Mathf.MoveTowards(animSpeed, target, dt * 4f);
        walkPhase += dt * (5f + animSpeed * 6f);
        float swing = 42f * animSpeed;
        float p = Mathf.Sin(walkPhase);

        if (lThigh) lThigh.localRotation = Quaternion.Euler(p * swing, 0, 0);
        if (rThigh) rThigh.localRotation = Quaternion.Euler(-p * swing, 0, 0);
        if (lShin) lShin.localRotation = Quaternion.Euler(Mathf.Max(0f, -p) * swing * 0.95f, 0, 0);
        if (rShin) rShin.localRotation = Quaternion.Euler(Mathf.Max(0f, p) * swing * 0.95f, 0, 0);
        if (armL) armL.localRotation = Quaternion.Euler(-18f + -p * swing * 0.35f, 0, 0);
        if (armR) armR.localRotation = Quaternion.Euler(-18f + p * swing * 0.35f, 0, 0);

        if (torso)
        {
            float idle = Mathf.Sin(Time.time * 1.6f) * 0.03f;
            float bounce = Mathf.Abs(Mathf.Sin(walkPhase)) * 0.08f * animSpeed;
            torso.localPosition = new Vector3(torso.localPosition.x, torsoBaseY + idle + bounce, torso.localPosition.z);
        }
        if (head) head.localRotation = Quaternion.Euler(Mathf.Sin(Time.time * 1.2f) * 3f, 0, 0);
    }

    void Die()
    {
        if (!Alive) return;
        Alive = false;
        moveInput = Vector3.zero;
        if (bar != null) bar.Hide();
        SfxManager.I?.Explosion();
        Fx.Blob(transform.position + Vector3.up * 1.2f, Data.accentColor, 0.8f, 3.2f, 0.4f);
        SpawnDebris();
        OnDeath?.Invoke(this);
        Destroy(gameObject, 0.05f);
    }

    void SpawnDebris()
    {
        Vector3 c = transform.position + Vector3.up * 1.2f;
        var mat = MechBuilder.Mat(Data.accentColor, 0.5f, 0.6f);
        for (int i = 0; i < 12; i++)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.transform.position = c + UnityEngine.Random.insideUnitSphere * 0.5f;
            g.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.15f, 0.4f);
            g.GetComponent<MeshRenderer>().sharedMaterial = i % 2 == 0 ? mat : MechBuilder.Mat(Data.bodyColor);
            var rb = g.AddComponent<Rigidbody>();
            rb.AddForce(Vector3.up * 3f + UnityEngine.Random.insideUnitSphere * 4f, ForceMode.Impulse);
            rb.AddTorque(UnityEngine.Random.insideUnitSphere * 8f, ForceMode.Impulse);
            Destroy(g, 2.2f);
        }
    }
}
