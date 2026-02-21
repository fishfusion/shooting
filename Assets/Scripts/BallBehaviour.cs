using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BallBehaviour : MonoBehaviour
{
    [SerializeField] private float minSpeedToSleep = 0.08f;
    [SerializeField] private float maxLifetime = 12f;

    private Rigidbody2D _rb;
    private float _spawnTime;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _spawnTime = Time.time;
    }

    private void Update()
    {
        if (Time.time > _spawnTime + maxLifetime)
        {
            Destroy(gameObject);
            return;
        }

        if (_rb.velocity.magnitude < minSpeedToSleep)
        {
            _rb.velocity = Vector2.zero;
            _rb.angularVelocity = 0f;
            _rb.Sleep();
        }
    }
}
