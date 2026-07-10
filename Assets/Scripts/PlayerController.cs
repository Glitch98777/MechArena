using UnityEngine;

// Drives the player's mech from the on-screen joystick + fire button.
public class PlayerController : MonoBehaviour
{
    Mech mech;
    public bool FireHeld;

    public void Bind(Mech m) { mech = m; }

    void Update()
    {
        if (mech == null || !mech.Alive) return;
        var ui = GameManager.Instance != null ? GameManager.Instance.UI : null;

        Vector2 j = ui != null ? ui.MoveAxis : Vector2.zero;
        // move relative to where the camera is looking, so "up" is always away from the camera
        Vector3 move = Quaternion.Euler(0, GameManager.Instance.CameraYaw, 0) * new Vector3(j.x, 0, j.y);
        mech.SetMove(move);

        bool fire = FireHeld || (ui != null && ui.FireHeld);
        if (fire)
        {
            // Only shoot enemies we actually have a clear line to.
            var target = GameManager.Instance.NearestVisibleEnemy(mech);
            if (target != null)
            {
                Vector3 aim = target.transform.position + Vector3.up * 1.4f;
                mech.SetFace(aim);
                mech.TryFire(aim);
            }
            else
            {
                var n = GameManager.Instance.NearestEnemy(mech);   // no clear shot: keep facing the nearest foe
                if (n != null) mech.SetFace(n.transform.position + Vector3.up * 1.4f);
            }
        }
    }
}
