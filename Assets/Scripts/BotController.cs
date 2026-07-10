using UnityEngine;

// Simple enemy AI: close to preferred range, strafe, and shoot at the nearest foe.
public class BotController : MonoBehaviour
{
    Mech mech;
    float preferredRange;
    int strafeDir;
    float strafeTimer;
    float lastHealth, damagedTimer, decisionTimer;

    public void Bind(Mech m, int seed)
    {
        mech = m;
        preferredRange = 9f + (seed % 3) * 3f;
        strafeDir = (seed % 2 == 0) ? 1 : -1;
        lastHealth = m.Health;
        decisionTimer = 1.2f + (seed % 3) * 0.4f;
    }

    void Update()
    {
        if (mech == null || !mech.Alive || GameManager.Instance == null) return;
        if (mech.Stunned) { mech.SetMove(Vector3.zero); return; }
        var target = GameManager.Instance.NearestEnemy(mech);
        if (target == null) { mech.SetMove(Vector3.zero); return; }

        Vector3 toTarget = target.transform.position - mech.transform.position;
        toTarget.y = 0;
        float dist = toTarget.magnitude;
        Vector3 dir = dist > 0.01f ? toTarget / dist : Vector3.forward;

        // ability use: mostly when hurt, sometimes at random in combat (cooldown-gated = not spammy)
        if (mech.Health < lastHealth - 0.5f) damagedTimer = 3f;
        lastHealth = mech.Health;
        if (damagedTimer > 0f) damagedTimer -= Time.deltaTime;
        decisionTimer -= Time.deltaTime;
        if (mech.AbilityReady && decisionTimer <= 0f)
        {
            decisionTimer = 1.5f;
            float chance = damagedTimer > 0f ? 0.5f : (dist < preferredRange + 8f ? 0.12f : 0f);
            if (Random.value < chance) mech.ActivateAbility();
        }

        strafeTimer -= Time.deltaTime;
        if (strafeTimer <= 0f) { strafeDir = -strafeDir; strafeTimer = Random.Range(2f, 4f); }

        Vector3 move;
        if (dist > preferredRange + 1.5f) move = dir;                                   // advance
        else if (dist < preferredRange - 2f) move = -dir;                                // back off
        else move = Vector3.Cross(Vector3.up, dir) * strafeDir;                          // circle

        mech.SetMove(move * 0.9f);

        Vector3 aim = target.transform.position + Vector3.up * 1.4f;
        mech.SetFace(aim);
        Vector3 eye = mech.transform.position + Vector3.up * 1.4f;
        if (dist < preferredRange + 6f && GameManager.Instance.HasLOS(eye, aim))
            mech.TryFire(aim);
    }
}
