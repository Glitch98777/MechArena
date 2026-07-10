using UnityEngine;

// Single MonoBehaviour placed in the scene. Everything else is created in code
// so the project needs no hand-authored prefabs, models, or UI assets.
public class Bootstrap : MonoBehaviour
{
    void Awake()
    {
        // Make sure only one game exists even on scene reload.
        if (GameManager.Instance != null) { Destroy(gameObject); return; }

        gameObject.name = "GameManager";
        gameObject.AddComponent<GameManager>();
    }
}
