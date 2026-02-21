using UnityEngine;

public class TargetSpawner : MonoBehaviour
{
    [SerializeField] private MovingTarget targetPrefab;
    [SerializeField] private int targetCount = 6;
    [SerializeField] private float xStart = -4.5f;
    [SerializeField] private float xGap = 1.8f;
    [SerializeField] private float yMin = 1.8f;
    [SerializeField] private float yMax = 4.3f;
    [SerializeField] private float zFixedDistance = 0f;

    private readonly float[] _scales = { 0.55f, 0.72f, 0.9f, 0.62f, 1.05f, 0.8f };
    private readonly float[] _speedFactors = { 0.75f, 1.2f, 0.95f, 1.5f, 0.85f, 1.35f };

    private void Start()
    {
        SpawnTargets();
    }

    [ContextMenu("Spawn Targets")]
    public void SpawnTargets()
    {
        ClearChildren();

        for (int i = 0; i < targetCount; i++)
        {
            float x = xStart + i * xGap;
            float normalized = targetCount <= 1 ? 0.5f : (float)i / (targetCount - 1);
            float y = Mathf.Lerp(yMin, yMax, normalized);

            MovingTarget target = Instantiate(targetPrefab, new Vector3(x, y, zFixedDistance), Quaternion.identity, transform);

            float speed = _speedFactors[i % _speedFactors.Length];
            Vector2 amp = new Vector2(0.5f + 0.45f * speed, 0.2f + 0.35f * speed);
            Vector2 freq = new Vector2(0.9f * speed, 0.7f * speed);
            Vector2 phase = new Vector2(i * 0.7f, i * 1.1f);
            float scale = _scales[i % _scales.Length];

            target.Configure(new Vector2(x, y), amp, freq, phase, scale);
        }
    }

    private void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
    }
}
