using UnityEngine;

public class MovingTarget : MonoBehaviour
{
    [Header("Path Settings")]
    [SerializeField] private Vector2 center;
    [SerializeField] private Vector2 amplitude = new Vector2(1.5f, 0.7f);
    [SerializeField] private Vector2 frequency = new Vector2(1.0f, 0.8f);
    [SerializeField] private Vector2 phaseOffset = new Vector2(0f, 0f);

    [Header("Optional Rotation")]
    [SerializeField] private float spinSpeed = 20f;

    public void Configure(Vector2 c, Vector2 amp, Vector2 freq, Vector2 phase, float scale)
    {
        center = c;
        amplitude = amp;
        frequency = freq;
        phaseOffset = phase;
        transform.localScale = new Vector3(scale, scale, 1f);
    }

    private void Update()
    {
        float t = Time.time;
        float x = center.x + Mathf.Sin(t * frequency.x + phaseOffset.x) * amplitude.x;
        float y = center.y + Mathf.Cos(t * frequency.y + phaseOffset.y) * amplitude.y;

        transform.position = new Vector3(x, y, transform.position.z);
        transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
    }
}
