using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class XPBDCloth : MonoBehaviour
{
    private struct DistanceConstraint
    {
        public int i0;
        public int i1;
        public float restLength;
        public float compliance;
    }

    [Header("Resolution")]
    [SerializeField] private int horizontalSegments = 28;
    [SerializeField] private int verticalSegments = 56;
    [SerializeField] private float clothWidth = 10f;
    [SerializeField] private float clothHeight = 20f;

    [Header("Dynamics")]
    [SerializeField] private float damping = 0.02f;
    [SerializeField] private float gravityScale = 1f;
    [SerializeField] private int substeps = 2;
    [SerializeField] private int solverIterations = 6;
    [SerializeField] private float stretchCompliance = 0.00002f;
    [SerializeField] private float shearCompliance = 0.00008f;

    [Header("Pinning")]
    [SerializeField] private bool pinTopEdge = true;

    [Header("Collisions")]
    [SerializeField] private bool collideWithCircleCollider2D = true;
    [SerializeField] private bool collideWithSphereCollider = true;
    [SerializeField] private float collisionMargin = 0.015f;
    [SerializeField] private float colliderRefreshInterval = 0.4f;

    [Header("Rendering")]
    [SerializeField] private Color clothColor = new Color(0.04f, 0.11f, 0.32f, 1f);
    [SerializeField] private float roughnessX = 0.22f;
    [SerializeField] private float roughnessY = 0.62f;
    [SerializeField] private float specularStrength = 0.95f;

    private Mesh _mesh;
    private Vector3[] _worldPos;
    private Vector3[] _prevWorldPos;
    private float[] _invMass;
    private Vector3[] _meshVertices;
    private readonly List<DistanceConstraint> _constraints = new List<DistanceConstraint>(4096);
    private float[] _lambdas;
    private CircleCollider2D[] _circle2DCache = System.Array.Empty<CircleCollider2D>();
    private SphereCollider[] _sphereCache = System.Array.Empty<SphereCollider>();
    private float _nextColliderRefreshTime;

    public void SetDimensions(float width, float height)
    {
        clothWidth = Mathf.Max(0.5f, width);
        clothHeight = Mathf.Max(1f, height);
    }

    public void RebuildNow()
    {
        BuildCloth();
    }

    private void Awake()
    {
        EnsureMaterial();
        BuildCloth();
    }

    private void OnValidate()
    {
        horizontalSegments = Mathf.Clamp(horizontalSegments, 2, 160);
        verticalSegments = Mathf.Clamp(verticalSegments, 2, 240);
        substeps = Mathf.Clamp(substeps, 1, 8);
        solverIterations = Mathf.Clamp(solverIterations, 1, 24);
        clothWidth = Mathf.Max(0.5f, clothWidth);
        clothHeight = Mathf.Max(1f, clothHeight);
        damping = Mathf.Clamp01(damping);
        colliderRefreshInterval = Mathf.Clamp(colliderRefreshInterval, 0.05f, 4f);
        collisionMargin = Mathf.Clamp(collisionMargin, 0.001f, 0.2f);
        EnsureMaterial();
    }

    private void Update()
    {
        if (_mesh == null || _worldPos == null || _worldPos.Length == 0) return;
        Simulate(Time.deltaTime);
        PushToMesh();
    }

    private void BuildCloth()
    {
        int columns = horizontalSegments + 1;
        int rows = verticalSegments + 1;
        int count = columns * rows;

        _worldPos = new Vector3[count];
        _prevWorldPos = new Vector3[count];
        _invMass = new float[count];
        _meshVertices = new Vector3[count];
        _constraints.Clear();

        float dx = clothWidth / horizontalSegments;
        float dy = clothHeight / verticalSegments;
        Vector2 uvStep = new Vector2(1f / horizontalSegments, 1f / verticalSegments);

        Vector2[] uv = new Vector2[count];

        for (int y = 0; y <= verticalSegments; y++)
        {
            for (int x = 0; x <= horizontalSegments; x++)
            {
                int i = x + y * columns;
                float lx = -clothWidth * 0.5f + x * dx;
                float ly = -y * dy;
                Vector3 world = transform.TransformPoint(new Vector3(lx, ly, 0f));

                _worldPos[i] = world;
                _prevWorldPos[i] = world;
                _invMass[i] = (pinTopEdge && y == 0) ? 0f : 1f;
                _meshVertices[i] = new Vector3(lx, ly, 0f);
                uv[i] = new Vector2(x * uvStep.x, 1f - y * uvStep.y);
            }
        }

        for (int y = 0; y <= verticalSegments; y++)
        {
            for (int x = 0; x <= horizontalSegments; x++)
            {
                int i = x + y * columns;
                if (x < horizontalSegments) AddConstraint(i, i + 1, stretchCompliance);
                if (y < verticalSegments) AddConstraint(i, i + columns, stretchCompliance);
                if (x < horizontalSegments && y < verticalSegments)
                {
                    AddConstraint(i, i + columns + 1, shearCompliance);
                    AddConstraint(i + 1, i + columns, shearCompliance);
                }
            }
        }

        _lambdas = new float[_constraints.Count];

        int[] tris = new int[horizontalSegments * verticalSegments * 6];
        int ti = 0;
        for (int y = 0; y < verticalSegments; y++)
        {
            for (int x = 0; x < horizontalSegments; x++)
            {
                int i = x + y * columns;
                int iRight = i + 1;
                int iDown = i + columns;
                int iDownRight = iDown + 1;

                tris[ti++] = i;
                tris[ti++] = iDownRight;
                tris[ti++] = iDown;
                tris[ti++] = i;
                tris[ti++] = iRight;
                tris[ti++] = iDownRight;
            }
        }

        if (_mesh == null)
        {
            _mesh = new Mesh { name = "XPBDClothMesh" };
            _mesh.MarkDynamic();
            GetComponent<MeshFilter>().sharedMesh = _mesh;
        }
        else
        {
            _mesh.Clear();
        }

        _mesh.vertices = _meshVertices;
        _mesh.uv = uv;
        _mesh.triangles = tris;
        _mesh.RecalculateNormals();
    }

    private void AddConstraint(int i0, int i1, float compliance)
    {
        float rest = Vector3.Distance(_worldPos[i0], _worldPos[i1]);
        _constraints.Add(new DistanceConstraint
        {
            i0 = i0,
            i1 = i1,
            restLength = rest,
            compliance = compliance
        });
    }

    private void Simulate(float dt)
    {
        if (dt <= 0f) return;
        if (Time.time >= _nextColliderRefreshTime)
        {
            RefreshColliders();
            _nextColliderRefreshTime = Time.time + colliderRefreshInterval;
        }

        float stepDt = dt / substeps;
        float dampingFactor = Mathf.Clamp01(1f - damping);
        Vector3 gravity = Physics.gravity * gravityScale;

        for (int s = 0; s < substeps; s++)
        {
            for (int i = 0; i < _worldPos.Length; i++)
            {
                if (_invMass[i] <= 0f) continue;
                Vector3 v = (_worldPos[i] - _prevWorldPos[i]) * dampingFactor;
                _prevWorldPos[i] = _worldPos[i];
                _worldPos[i] += v + gravity * (stepDt * stepDt);
            }

            System.Array.Clear(_lambdas, 0, _lambdas.Length);
            for (int iter = 0; iter < solverIterations; iter++)
            {
                SolveDistanceConstraints(stepDt);
                SolveBallCollisions();
            }
        }
    }

    private void SolveDistanceConstraints(float stepDt)
    {
        float dt2 = stepDt * stepDt;
        for (int c = 0; c < _constraints.Count; c++)
        {
            DistanceConstraint dc = _constraints[c];
            float w0 = _invMass[dc.i0];
            float w1 = _invMass[dc.i1];
            if (w0 + w1 <= 0f) continue;

            Vector3 d = _worldPos[dc.i0] - _worldPos[dc.i1];
            float len = d.magnitude;
            if (len < 0.000001f) continue;
            Vector3 n = d / len;
            float cVal = len - dc.restLength;
            float alpha = dc.compliance / dt2;

            float dl = (-cVal - alpha * _lambdas[c]) / (w0 + w1 + alpha);
            _lambdas[c] += dl;

            _worldPos[dc.i0] += n * (dl * w0);
            _worldPos[dc.i1] -= n * (dl * w1);
        }
    }

    private void SolveBallCollisions()
    {
        float push = Mathf.Max(0f, collisionMargin);
        if (collideWithCircleCollider2D && _circle2DCache.Length > 0)
        {
            for (int i = 0; i < _worldPos.Length; i++)
            {
                if (_invMass[i] <= 0f) continue;
                Vector3 p = _worldPos[i];
                Vector2 p2 = new Vector2(p.x, p.y);
                for (int j = 0; j < _circle2DCache.Length; j++)
                {
                    CircleCollider2D cc = _circle2DCache[j];
                    if (cc == null || !cc.enabled || !cc.gameObject.activeInHierarchy) continue;

                    Vector2 center = cc.transform.TransformPoint(cc.offset);
                    Vector3 ls = cc.transform.lossyScale;
                    float scale = Mathf.Max(Mathf.Abs(ls.x), Mathf.Abs(ls.y));
                    float radius = cc.radius * scale + push;
                    Vector2 d = p2 - center;
                    float dist = d.magnitude;
                    if (dist < radius && dist > 0.000001f)
                    {
                        Vector2 n = d / dist;
                        Vector2 corrected = center + n * radius;
                        p.x = corrected.x;
                        p.y = corrected.y;
                        p2 = corrected;
                    }
                }
                _worldPos[i] = p;
            }
        }

        if (collideWithSphereCollider && _sphereCache.Length > 0)
        {
            for (int i = 0; i < _worldPos.Length; i++)
            {
                if (_invMass[i] <= 0f) continue;
                Vector3 p = _worldPos[i];
                for (int j = 0; j < _sphereCache.Length; j++)
                {
                    SphereCollider sc = _sphereCache[j];
                    if (sc == null || !sc.enabled || !sc.gameObject.activeInHierarchy) continue;

                    Vector3 center = sc.transform.TransformPoint(sc.center);
                    Vector3 ls = sc.transform.lossyScale;
                    float scale = Mathf.Max(Mathf.Abs(ls.x), Mathf.Abs(ls.y), Mathf.Abs(ls.z));
                    float radius = sc.radius * scale + push;
                    Vector3 d = p - center;
                    float dist = d.magnitude;
                    if (dist < radius && dist > 0.000001f)
                    {
                        p = center + d / dist * radius;
                    }
                }
                _worldPos[i] = p;
            }
        }
    }

    private void RefreshColliders()
    {
        if (collideWithCircleCollider2D)
        {
#if UNITY_2022_2_OR_NEWER
            _circle2DCache = FindObjectsByType<CircleCollider2D>(FindObjectsSortMode.None);
#else
            _circle2DCache = FindObjectsOfType<CircleCollider2D>();
#endif
        }

        if (collideWithSphereCollider)
        {
#if UNITY_2022_2_OR_NEWER
            _sphereCache = FindObjectsByType<SphereCollider>(FindObjectsSortMode.None);
#else
            _sphereCache = FindObjectsOfType<SphereCollider>();
#endif
        }
    }

    private void PushToMesh()
    {
        for (int i = 0; i < _worldPos.Length; i++)
        {
            _meshVertices[i] = transform.InverseTransformPoint(_worldPos[i]);
        }
        _mesh.vertices = _meshVertices;
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();
    }

    private void EnsureMaterial()
    {
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr.sharedMaterial == null)
        {
            Shader ward = Shader.Find("Custom/WardCloth");
            if (ward != null)
            {
                mr.sharedMaterial = new Material(ward);
            }
        }

        Material mat = mr.sharedMaterial;
        if (mat == null) return;
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", clothColor);
        if (mat.HasProperty("_RoughnessX")) mat.SetFloat("_RoughnessX", roughnessX);
        if (mat.HasProperty("_RoughnessY")) mat.SetFloat("_RoughnessY", roughnessY);
        if (mat.HasProperty("_SpecularStrength")) mat.SetFloat("_SpecularStrength", specularStrength);
    }
}
