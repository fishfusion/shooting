using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private ShooterController shooter;
    [SerializeField] private TargetSpawner targetSpawner;
    [SerializeField] private Transform shooterTransform;
    [SerializeField] private Collider2D groundCollider;
    [SerializeField] private XPBDCloth backdropCloth;

    [Header("Bottom Anchor")]
    [SerializeField] private float shooterBottomOffset = 0.8f;

    [Header("Backdrop Cloth")]
    [SerializeField] private float clothTopMargin = 0.35f;
    [SerializeField] private float clothZOffset = 6f;

    private void Start()
    {
        PositionShooterAtBottom();
        EnsureBackdropReferences();
        ConfigureBackdropCloth();

        if (targetSpawner != null)
        {
            targetSpawner.SpawnTargets();
        }

        Application.targetFrameRate = 60;
    }

    private void EnsureBackdropReferences()
    {
        if (groundCollider == null)
        {
#if UNITY_2022_2_OR_NEWER
            groundCollider = FindFirstObjectByType<Collider2D>();
#else
            groundCollider = FindObjectOfType<Collider2D>();
#endif
        }

        if (backdropCloth == null)
        {
#if UNITY_2022_2_OR_NEWER
            backdropCloth = FindFirstObjectByType<XPBDCloth>();
#else
            backdropCloth = FindObjectOfType<XPBDCloth>();
#endif
            if (backdropCloth == null)
            {
                GameObject go = new GameObject("BackdropCloth");
                go.transform.SetParent(transform, false);
                go.AddComponent<MeshFilter>();
                go.AddComponent<MeshRenderer>();
                backdropCloth = go.AddComponent<XPBDCloth>();
            }
        }
    }

    private void PositionShooterAtBottom()
    {
        if (shooterTransform == null) return;
        Camera cam = Camera.main;
        if (cam == null || !cam.orthographic) return;

        float halfHeight = cam.orthographicSize;
        float bottomY = cam.transform.position.y - halfHeight + shooterBottomOffset;
        Vector3 pos = shooterTransform.position;
        shooterTransform.position = new Vector3(pos.x, bottomY, pos.z);
    }

    private void ConfigureBackdropCloth()
    {
        if (backdropCloth == null || groundCollider == null) return;

        float clothWidth = groundCollider.bounds.size.x;
        float clothHeight = clothWidth * 2f;
        backdropCloth.SetDimensions(clothWidth, clothHeight);

        float topY = groundCollider.bounds.max.y + clothHeight + 0.25f;
        Camera cam = Camera.main;
        if (cam != null && cam.orthographic)
        {
            topY = cam.transform.position.y + cam.orthographicSize - clothTopMargin;
        }

        backdropCloth.transform.position = new Vector3(groundCollider.bounds.center.x, topY, clothZOffset);
        backdropCloth.RebuildNow();
    }
}
