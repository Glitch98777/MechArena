using UnityEngine;

// Data for a purchasable/equippable ability. Each chassis's own signature ability is free on that chassis;
// any other ability must be bought once (then it can be equipped on any mech).
public class AbilityDef
{
    public AbilityType type;
    public string name;
    public string desc;
    public float cooldown;
    public int price;
    public Color color;

    public static AbilityDef Get(AbilityType t)
    {
        switch (t)
        {
            case AbilityType.Dash: return new AbilityDef { type = t, name = "BLINK", desc = "Burst dash + hop", cooldown = 5f, price = 300, color = new Color(.6f, 1f, .6f) };
            case AbilityType.Slam: return new AbilityDef { type = t, name = "SLAM", desc = "Shockwave AoE (65)", cooldown = 11f, price = 600, color = new Color(1f, .7f, .3f) };
            case AbilityType.PhaseStrike: return new AbilityDef { type = t, name = "PHANTOM", desc = "Blink to foe, strike (85)", cooldown = 9f, price = 700, color = new Color(.8f, .5f, 1f) };
            case AbilityType.Tesla: return new AbilityDef { type = t, name = "TESLA", desc = "Insta-kill nearest foe", cooldown = 20f, price = 1100, color = new Color(.6f, .9f, 1f) };
            case AbilityType.Heal: return new AbilityDef { type = t, name = "REPAIR", desc = "Restore 55% hull", cooldown = 18f, price = 550, color = new Color(.4f, 1f, .5f) };
            case AbilityType.Overdrive: return new AbilityDef { type = t, name = "OVERDRIVE", desc = "6s: 2x fire + speed", cooldown = 15f, price = 700, color = new Color(1f, .8f, .3f) };
            case AbilityType.Cloak: return new AbilityDef { type = t, name = "CLOAK", desc = "5s untargetable", cooldown = 14f, price = 750, color = new Color(.6f, .8f, 1f) };
            case AbilityType.Barrage: return new AbilityDef { type = t, name = "BARRAGE", desc = "Airstrike on nearest area", cooldown = 16f, price = 800, color = new Color(1f, .5f, .25f) };
            case AbilityType.EMP: return new AbilityDef { type = t, name = "EMP", desc = "Stun nearby enemies 3s", cooldown = 14f, price = 750, color = new Color(.5f, .9f, 1f) };
            default: return new AbilityDef { type = AbilityType.Shield, name = "AEGIS", desc = "15s invincibility shield", cooldown = 22f, price = 900, color = new Color(.3f, .7f, 1f) };
        }
    }

    public static readonly AbilityType[] All =
    {
        AbilityType.Shield, AbilityType.Dash, AbilityType.Tesla, AbilityType.Slam, AbilityType.PhaseStrike,
        AbilityType.Heal, AbilityType.Overdrive, AbilityType.Cloak, AbilityType.Barrage, AbilityType.EMP
    };
}
