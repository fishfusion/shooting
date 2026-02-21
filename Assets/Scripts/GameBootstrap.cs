using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private ShooterController shooter;
    [SerializeField] private TargetSpawner targetSpawner;
    [SerializeField] private Transform shooterTransform;

    [Header("Bottom Anchor")]
    [SerializeField] private float shooterBottomOffset = 0.8f;

    private void Start()
    {
        PositionShooterAtBottom();

        if (targetSpawner != null)
        {
            targetSpawner.SpawnTargets();
        }

        Application.targetFrameRate = 60;
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
}
