using UnityEngine;

public enum Wep { Blaster, Cannon, Shotgun, Rocket, Laser, Minigun, Railgun, Plasma, Flak, Arc, Oblivion, Inferno, Stormcaller, Annihilator, Voidripper, Singularity, Apocalypse }

// Defines how a weapon fires. Chassis and weapon are chosen separately.
public class WeaponDef
{
    public Wep id;
    public string name;
    public string desc;
    public float damage;
    public float fireRate;       // shots per second
    public float projectileSpeed;
    public float projectileSize = 0.28f;
    public int pellets = 1;      // >1 = spread
    public float spreadDeg = 0f;
    public float splashRadius = 0f;
    public bool beam = false;    // hitscan
    public float range = 60f;
    public Color color = Color.cyan;
    public int price = 0;        // credits to unlock (0 = starter)
    public int reqRank = 0;      // rank required to buy (0 = none). Legendary gear is gated by rank AND price.

    public static WeaponDef Get(Wep w)
    {
        var d = GetRaw(w);
        if (d.reqRank > 0)   // legendaries: slightly cheaper + slightly lower rank
        {
            d.reqRank = Mathf.Max(1, d.reqRank - 2);
            d.price = Mathf.Max(0, Mathf.RoundToInt(d.price * 0.85f));
        }
        return d;
    }

    static WeaponDef GetRaw(Wep w)
    {
        switch (w)
        {
            case Wep.Cannon: return new WeaponDef {
                id = w, name = "CANNON", desc = "Slow, heavy single slug",
                damage = 34f, fireRate = 1.1f, projectileSpeed = 30f, projectileSize = 0.5f,
                color = new Color(1f, 0.7f, 0.2f), price = 400 };
            case Wep.Shotgun: return new WeaponDef {
                id = w, name = "SCATTER", desc = "5-pellet close spread",
                damage = 7f, fireRate = 1.3f, projectileSpeed = 34f, projectileSize = 0.24f,
                pellets = 5, spreadDeg = 16f, range = 30f,
                color = new Color(1f, 0.45f, 0.5f), price = 500 };
            case Wep.Rocket: return new WeaponDef {
                id = w, name = "ROCKET", desc = "Splash damage on impact",
                damage = 22f, fireRate = 1.0f, projectileSpeed = 24f, projectileSize = 0.42f,
                splashRadius = 4.5f, color = new Color(1f, 0.55f, 0.25f), price = 650 };
            case Wep.Laser: return new WeaponDef {
                id = w, name = "LANCE", desc = "Instant-hit beam",
                damage = 9f, fireRate = 4.5f, projectileSpeed = 0f, beam = true, range = 55f,
                color = new Color(0.5f, 1f, 0.9f), price = 800 };
            case Wep.Minigun: return new WeaponDef {
                id = w, name = "MINIGUN", desc = "Ultra-fast bullet hose",
                damage = 4.5f, fireRate = 9f, projectileSpeed = 44f, projectileSize = 0.2f,
                spreadDeg = 5f, pellets = 1, color = new Color(1f, 0.85f, 0.4f), price = 600 };
            case Wep.Railgun: return new WeaponDef {
                id = w, name = "RAILGUN", desc = "Devastating high-velocity slug",
                damage = 58f, fireRate = 0.7f, projectileSpeed = 62f, projectileSize = 0.55f,
                color = new Color(0.6f, 0.9f, 1f), price = 950 };
            case Wep.Plasma: return new WeaponDef {
                id = w, name = "PLASMA", desc = "Small splash energy bolts",
                damage = 15f, fireRate = 2.1f, projectileSpeed = 30f, projectileSize = 0.4f,
                splashRadius = 2.6f, color = new Color(0.5f, 1f, 0.5f), price = 700 };
            case Wep.Flak: return new WeaponDef {
                id = w, name = "FLAK", desc = "4-pellet flak burst",
                damage = 9f, fireRate = 1.7f, projectileSpeed = 32f, projectileSize = 0.26f,
                pellets = 4, spreadDeg = 22f, range = 34f, color = new Color(1f, 0.7f, 0.35f), price = 550 };
            case Wep.Arc: return new WeaponDef {
                id = w, name = "ARC", desc = "Rapid energy beam",
                damage = 13f, fireRate = 3.0f, beam = true, range = 50f,
                color = new Color(0.8f, 0.5f, 1f), price = 850 };

            // ---- LEGENDARY (rank + big-credit gated, deliberately overpowered) ----
            case Wep.Oblivion: return new WeaponDef {
                id = w, name = "OBLIVION", desc = "[LEGENDARY] Anti-mech siege slug",
                damage = 120f, fireRate = 1.3f, projectileSpeed = 70f, projectileSize = 0.72f,
                color = new Color(0.8f, 1f, 1f), price = 5000, reqRank = 8 };
            case Wep.Inferno: return new WeaponDef {
                id = w, name = "INFERNO", desc = "[LEGENDARY] Rapid heavy rockets, huge splash",
                damage = 55f, fireRate = 1.9f, projectileSpeed = 32f, projectileSize = 0.5f,
                splashRadius = 6.5f, color = new Color(1f, 0.5f, 0.2f), price = 9000, reqRank = 12 };
            case Wep.Stormcaller: return new WeaponDef {
                id = w, name = "STORMCALLER", desc = "[LEGENDARY] Overcharged instant beam",
                damage = 44f, fireRate = 5f, beam = true, range = 64f,
                color = new Color(0.8f, 0.6f, 1f), price = 14000, reqRank = 16 };
            case Wep.Annihilator: return new WeaponDef {
                id = w, name = "ANNIHILATOR", desc = "[LEGENDARY] Endless gold barrage",
                damage = 27f, fireRate = 13f, projectileSpeed = 54f, projectileSize = 0.34f,
                color = new Color(1f, 0.85f, 0.3f), price = 26000, reqRank = 20 };
            case Wep.Voidripper: return new WeaponDef {
                id = w, name = "VOIDRIPPER", desc = "[MYTHIC] Void rockets, colossal splash",
                damage = 72f, fireRate = 1.5f, projectileSpeed = 32f, projectileSize = 0.58f,
                splashRadius = 8f, color = new Color(0.6f, 0.3f, 1f), price = 32000, reqRank = 24 };
            case Wep.Singularity: return new WeaponDef {
                id = w, name = "SINGULARITY", desc = "[MYTHIC] Continuous devastating beam",
                damage = 62f, fireRate = 5f, beam = true, range = 68f,
                color = new Color(1f, 0.4f, 0.9f), price = 46000, reqRank = 28 };
            case Wep.Apocalypse: return new WeaponDef {
                id = w, name = "APOCALYPSE", desc = "[MYTHIC] Storm of fire",
                damage = 34f, fireRate = 14f, projectileSpeed = 56f, projectileSize = 0.36f,
                color = new Color(1f, 0.5f, 0.2f), price = 62000, reqRank = 32 };

            default: return new WeaponDef {
                id = Wep.Blaster, name = "BLASTER", desc = "Balanced rapid-fire",
                damage = 11f, fireRate = 3.2f, projectileSpeed = 38f, projectileSize = 0.28f,
                color = new Color(0.4f, 0.85f, 1f), price = 0 };
        }
    }

    public static readonly Wep[] All =
        { Wep.Blaster, Wep.Cannon, Wep.Shotgun, Wep.Rocket, Wep.Laser, Wep.Minigun, Wep.Railgun, Wep.Plasma, Wep.Flak, Wep.Arc,
          Wep.Oblivion, Wep.Inferno, Wep.Stormcaller, Wep.Annihilator, Wep.Voidripper, Wep.Singularity, Wep.Apocalypse };
}
