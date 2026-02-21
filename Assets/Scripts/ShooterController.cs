using UnityEngine;

public class ShooterController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Rigidbody2D ballPrefab;
    [SerializeField] private Transform muzzlePoint;

    [Header("Shot Settings")]
    [SerializeField] private float maxDragDistance = 3.0f;
    [SerializeField] private float forceScale = 8.0f;
    [SerializeField] private float fireCooldown = 0.2f;

    [Header("Aiming Visual")]
    [SerializeField] private LineRenderer aimLine;

    private bool _isDragging;
    private Vector2 _dragStartWorld;
    private Vector2 _currentDragWorld;
    private float _lastShotTime = -999f;

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (aimLine != null)
        {
            aimLine.positionCount = 2;
            aimLine.enabled = false;
        }
    }

    private void Update()
    {
        HandleTouchOrMouseInput();
        UpdateAimVisual();
    }

    private void HandleTouchOrMouseInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            Vector2 worldPos = ScreenToWorld(touch.position);

            if (touch.phase == TouchPhase.Began)
            {
                BeginDrag(worldPos);
            }
            else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                ContinueDrag(worldPos);
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                EndDragAndShoot();
            }

            return;
        }

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL
        if (Input.GetMouseButtonDown(0))
        {
            BeginDrag(ScreenToWorld(Input.mousePosition));
        }
        else if (Input.GetMouseButton(0))
        {
            ContinueDrag(ScreenToWorld(Input.mousePosition));
        }
        else if (Input.GetMouseButtonUp(0))
        {
            EndDragAndShoot();
        }
#endif
    }

    private void BeginDrag(Vector2 worldPos)
    {
        if (Time.time < _lastShotTime + fireCooldown)
        {
            return;
        }

        _isDragging = true;
        _dragStartWorld = muzzlePoint.position;
        _currentDragWorld = worldPos;
    }

    private void ContinueDrag(Vector2 worldPos)
    {
        if (!_isDragging) return;
        _currentDragWorld = worldPos;
    }

    private void EndDragAndShoot()
    {
        if (!_isDragging) return;
        _isDragging = false;

        Vector2 rawDrag = _dragStartWorld - _currentDragWorld;
        Vector2 clampedDrag = Vector2.ClampMagnitude(rawDrag, maxDragDistance);

        if (clampedDrag.sqrMagnitude < 0.01f)
        {
            if (aimLine != null) aimLine.enabled = false;
            return;
        }

        FireBall(clampedDrag * forceScale);
        _lastShotTime = Time.time;

        if (aimLine != null) aimLine.enabled = false;
    }

    private void FireBall(Vector2 impulse)
    {
        Rigidbody2D ball = Instantiate(ballPrefab, muzzlePoint.position, Quaternion.identity);
        ball.AddForce(impulse, ForceMode2D.Impulse);
    }

    private void UpdateAimVisual()
    {
        if (aimLine == null) return;

        if (!_isDragging)
        {
            aimLine.enabled = false;
            return;
        }

        Vector2 rawDrag = _dragStartWorld - _currentDragWorld;
        Vector2 clampedDrag = Vector2.ClampMagnitude(rawDrag, maxDragDistance);
        Vector2 direction = clampedDrag.normalized;
        float strength01 = Mathf.Clamp01(clampedDrag.magnitude / maxDragDistance);

        aimLine.enabled = true;
        aimLine.SetPosition(0, muzzlePoint.position);
        aimLine.SetPosition(1, (Vector2)muzzlePoint.position + direction * (1f + strength01 * 2f));
    }

    private Vector2 ScreenToWorld(Vector2 screenPos)
    {
        Vector3 world = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -mainCamera.transform.position.z));
        return world;
    }
}
