using UnityEngine;

// Projectile with optional splash. Hits resolved by distance to enemy mechs (no colliders).
public class Projectile : MonoBehaviour
{
    float speed, damage, life = 3.5f, splash;
    int team;
    Color color;

    public static Projectile Spawn(Vector3 pos, Vector3 dir, float speed, float damage,
                                   float size, float splash, int team, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Shot";
        Object.Destroy(go.GetComponent<Collider>());
        go.transform.position = pos;
        go.transform.localScale = new Vector3(size, size, size * 1.6f); // slight streak
        go.GetComponent<MeshRenderer>().sharedMaterial = MechBuilder.EmissiveMat(color);

        var p = go.AddComponent<Projectile>();
        p.speed = speed; p.damage = damage; p.team = team; p.splash = splash; p.color = color;
        p.transform.forward = dir.sqrMagnitude > 0.001f ? dir.normalized : Vector3.forward;
        return p;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        life -= dt;
        Vector3 prev = transform.position;
        transform.position += transform.forward * speed * dt;

        // blocked by cover / obstacle?
        if (Physics.Linecast(prev, transform.position, out var hit, GameManager.ObstacleMask))
        {
            Fx.Blob(hit.point, color, 0.14f, 0.6f, 0.12f);
            Destroy(gameObject);
            return;
        }

        if (GameManager.Instance != null)
        {
            var mechs = GameManager.Instance.Mechs;
            for (int i = 0; i < mechs.Count; i++)
            {
                var m = mechs[i];
                if (m == null || !m.Alive || m.Team == team) continue;
                Vector3 center = m.transform.position + Vector3.up * 1.4f;
                if ((center - transform.position).sqrMagnitude < 1.8f * 1.8f)
                {
                    Detonate(m);
                    return;
                }
            }
        }

        if (life <= 0f || Mathf.Abs(transform.position.x) > 90f || Mathf.Abs(transform.position.z) > 90f)
            Destroy(gameObject);
    }

    void Detonate(Mech firstHit)
    {
        if (splash > 0f)
        {
            Fx.Blob(transform.position, color, splash * 0.4f, splash * 1.3f, 0.28f);
            var mechs = GameManager.Instance.Mechs;
            for (int i = 0; i < mechs.Count; i++)
            {
                var m = mechs[i];
                if (m == null || !m.Alive || m.Team == team) continue;
                float d = Vector3.Distance(m.transform.position + Vector3.up * 1.4f, transform.position);
                if (d <= splash)
                    m.TakeDamage(damage * Mathf.Lerp(1f, 0.4f, d / splash));
            }
        }
        else
        {
            firstHit.TakeDamage(damage);
            Fx.Blob(transform.position, color, 0.2f, 0.8f, 0.14f);
        }
        Destroy(gameObject);
    }
}
