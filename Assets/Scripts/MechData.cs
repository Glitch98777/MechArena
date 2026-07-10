using UnityEngine;

public enum AbilityType { Shield, Dash, Tesla, Slam, PhaseStrike, Heal, Overdrive, Cloak, Barrage, EMP }

// Plain data describing a mech chassis. Drives the procedural model, stats, and signature ability.
[System.Serializable]
public class MechData
{
    public string name;
    public string role;
    public Color bodyColor;
    public Color accentColor;

    public float bodyWidth = 1.2f;
    public float bodyHeight = 1.2f;
    public float bodyDepth = 0.8f;
    public float legLength = 1.1f;
    public float legThick = 0.35f;
    public float armThick = 0.35f;
    public float heightScale = 1f;

    // Visual variety
    public int headStyle;      // 0 box+visor, 1 round scout, 2 wide horned
    public int shoulderStyle;  // 0 pauldron, 1 slim, 2 spiked

    public float maxHealth = 100f;
    public float moveSpeed = 4f;
    public float turnSpeed = 200f;
    public int weaponType = 0;

    // Signature ability
    public AbilityType ability;
    public string abilityName = "ABILITY";
    public string abilityDesc = "";
    public float abilityCooldown = 12f;

    public int price;      // credits to unlock (0 = starter, always owned)
    public int reqRank;    // rank required to buy (0 = none)
    public float teslaResistDrop; // >0 = survives one Tesla insta-kill, dropping to this fraction of max HP

    static MechData M(string name, string role, Color body, Color acc,
        float bw, float bh, float bd, float legLen, float legT, float armT, float hs,
        int head, int shoulder, float hp, float spd, float turn,
        AbilityType ab, string abName, string abDesc, float abCd, int price, int reqRank = 0, float teslaResistDrop = 0f)
    {
        return new MechData {
            name = name, role = role, bodyColor = body, accentColor = acc,
            bodyWidth = bw, bodyHeight = bh, bodyDepth = bd, legLength = legLen, legThick = legT, armThick = armT, heightScale = hs,
            headStyle = head, shoulderStyle = shoulder, maxHealth = hp, moveSpeed = spd, turnSpeed = turn,
            ability = ab, abilityName = abName, abilityDesc = abDesc, abilityCooldown = abCd, price = price, reqRank = reqRank, teslaResistDrop = teslaResistDrop
        };
    }

    // First three chassis are free starters; the rest cost credits (stronger = pricier).
    public static MechData[] Catalog()
    {
        Color C(float r, float g, float b) => new Color(r, g, b);
        var list = new MechData[]
        {
            M("TITAN","Heavy Assault", C(.75f,.18f,.18f), C(1f,.6f,.2f), 1.5f,1.4f,1f,1.1f,.45f,.45f,1.15f, 2,2, 170,3.4f,150, AbilityType.Shield,"AEGIS","15s invincibility shield",24f, 0),
            M("RAPTOR","Recon Striker", C(.15f,.6f,.3f), C(.6f,1f,.5f), 1f,1.1f,.7f,1.4f,.28f,.28f,.95f, 1,1, 95,6.5f,280, AbilityType.Dash,"BLINK","Burst dash + hop",5f, 0),
            M("VANGUARD","Balanced Line", C(.2f,.4f,.8f), C(.5f,.85f,1f), 1.25f,1.25f,.85f,1.15f,.36f,.36f,1f, 0,0, 125,4.6f,210, AbilityType.Tesla,"TESLA","Lightning insta-kills nearest foe",20f, 0),
            M("GOLIATH","Siege Bruiser", C(.85f,.5f,.12f), C(1f,.85f,.3f), 1.7f,1.5f,1.1f,1f,.5f,.5f,1.25f, 2,2, 210,2.9f,130, AbilityType.Slam,"SIEGE SLAM","Shockwave damages all nearby",11f, 600),
            M("WRAITH","Phantom Duelist", C(.45f,.2f,.7f), C(.85f,.5f,1f), 1.05f,1.15f,.75f,1.3f,.3f,.3f,1f, 1,1, 105,5.5f,250, AbilityType.PhaseStrike,"PHANTOM","Blink to foe, heavy strike",9f, 750),
            M("NOVA","Pyro Lancer", C(.8f,.2f,.1f), C(1f,.8f,.2f), 1.2f,1.25f,.8f,1.15f,.34f,.34f,1.05f, 0,2, 130,4.4f,200, AbilityType.Slam,"FIRESTORM","Blast wave scorches nearby",12f, 650),
            M("FROST","Cryo Sentinel", C(.3f,.55f,.75f), C(.7f,.95f,1f), 1.4f,1.35f,.95f,1.1f,.42f,.42f,1.1f, 0,0, 160,3.6f,170, AbilityType.Shield,"CRYO WALL","Frozen invincibility barrier",22f, 800),
            M("VIPER","Venom Skirmisher", C(.4f,.7f,.1f), C(.8f,1f,.2f), .95f,1.05f,.7f,1.4f,.27f,.27f,.92f, 1,1, 90,6.8f,290, AbilityType.Dash,"STRIKE DASH","Rapid venom dash",5f, 700),
            M("HAVOC","Chaos Bruiser", C(.7f,.15f,.55f), C(1f,.4f,.9f), 1.6f,1.45f,1.05f,1.02f,.48f,.48f,1.2f, 2,2, 195,3.1f,140, AbilityType.Slam,"HAVOC SLAM","Ground pound AoE",11f, 900),
            M("SABLE","Night Stalker", C(.12f,.12f,.18f), C(.6f,.35f,1f), 1.05f,1.15f,.78f,1.3f,.3f,.3f,1f, 1,1, 110,5.6f,255, AbilityType.PhaseStrike,"SHADOWSTEP","Teleport strike from shadow",8f, 1100),
            M("AURUM","Gilded Vanguard", C(.85f,.65f,.1f), C(1f,.9f,.5f), 1.3f,1.3f,.9f,1.15f,.38f,.38f,1.05f, 0,0, 135,4.5f,205, AbilityType.Tesla,"ION SURGE","Golden lightning insta-kill",19f, 1200),
            M("ONYX","Black Ops", C(.1f,.11f,.14f), C(1f,.3f,.3f), 1.15f,1.2f,.82f,1.25f,.32f,.32f,1.02f, 1,2, 115,5.2f,240, AbilityType.Dash,"BLACKOUT","Combat dash + reposition",6f, 850),

            // ---- LEGENDARY (rank + big-credit gated, deliberately overpowered) ----
            M("OVERLORD","[LEGENDARY] Warlord", C(.5f,.1f,.1f), C(1f,.75f,.2f), 1.7f,1.5f,1.1f,1.15f,.5f,.5f,1.3f, 2,2, 340,5.0f,220, AbilityType.Tesla,"ION SURGE","Insta-kill nearest foe",14f, 6000, 10),
            M("SERAPH","[LEGENDARY] Sky Sovereign", C(.9f,.9f,.95f), C(.6f,.85f,1f), 1.3f,1.35f,.9f,1.35f,.36f,.36f,1.15f, 1,0, 270,6.6f,290, AbilityType.Shield,"AEGIS","15s invincibility shield",14f, 12000, 15),
            M("APEX","[LEGENDARY] Apex Titan", C(.15f,.15f,.2f), C(1f,.4f,1f), 1.55f,1.5f,1.05f,1.3f,.44f,.44f,1.25f, 2,2, 420,6.2f,270, AbilityType.Tesla,"ANNIHILATE","Insta-kill nearest foe",10f, 30000, 22, 0.10f),
            M("TEMPEST","[MYTHIC] Storm Sovereign", C(.1f,.3f,.6f), C(.4f,.9f,1f), 1.3f,1.35f,.9f,1.4f,.38f,.38f,1.15f, 1,2, 320,6.9f,300, AbilityType.PhaseStrike,"TEMPEST STRIKE","Blink strike from the storm",7f, 34000, 25),
            M("COLOSSUS","[MYTHIC] Fortress", C(.35f,.35f,.4f), C(1f,.7f,.2f), 1.9f,1.6f,1.2f,1.1f,.55f,.55f,1.35f, 2,2, 520,3.6f,140, AbilityType.Shield,"BULWARK","15s invincibility barrier",15f, 44000, 28, 0.25f),
            M("OMEGA","[MYTHIC] Omega Prime", C(.08f,.08f,.12f), C(1f,.35f,.85f), 1.6f,1.55f,1.1f,1.35f,.46f,.46f,1.3f, 2,2, 480,6.6f,290, AbilityType.Tesla,"OMEGA SURGE","Insta-kill nearest foe",9f, 60000, 32, 0.25f),
        };
        // legendaries: slightly cheaper + slightly lower rank
        foreach (var d in list)
            if (d.reqRank > 0)
            {
                d.reqRank = Mathf.Max(1, d.reqRank - 2);
                d.price = Mathf.Max(0, Mathf.RoundToInt(d.price * 0.85f));
            }
        return list;
    }
}
