using System.Collections.Generic;
using UnityEngine;

public class HiZOccludee : MonoBehaviour, IHiZOccludee
{
    [SerializeField]
    private List<Renderer> renderers = new();

    [SerializeField]
    private Bounds bounds;

    private OccludeeDataCSInput _csInput;
    private int _index = -1;
    private bool _isDirty;
    private Vector3 _oldPosition = Vector3.zero;
    private Quaternion _oldRotation = Quaternion.identity;

    private Transform _transform;

    private uint isVisible = uint.MaxValue;

    private void Awake()
    {
        _transform = transform;

        renderers.AddRange(GetComponentsInChildren<MeshRenderer>());
        renderers.AddRange(GetComponentsInChildren<SkinnedMeshRenderer>());

        if (renderers.Count == 0)
        {
            return;
        }

        calculateBounds();

        if (Hi_Z_OcclusionCulling.instance != default)
        {
            Hi_Z_OcclusionCulling.instance.AddOccludee(this);
        }
    }

    private void Update()
    {
        updateBounds();
    }

    private void FixedUpdate()
    {
        _isDirty = false;
    }

    private void OnDestroy()
    {
        if (renderers == default || renderers.Count == 0)
        {
            return;
        }

        if (Hi_Z_OcclusionCulling.instance != default)
        {
            Hi_Z_OcclusionCulling.instance.RemoveOccludee(this);
        }
    }

    private void OnDrawGizmos()
    {
        switch (isVisible)
        {
            case 0:
                Gizmos.color = Color.red;
                break;
            case 1:
                Gizmos.color = Color.green;
                break;
            case 2:
                Gizmos.color = Color.blue;
                break;
            case 3:
                Gizmos.color = Color.white;
                break;
            case 4:
                Gizmos.color = Color.black;
                break;
        }

        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }


    public int Index
    {
        get => _index;
        set
        {
            if (_index == value)
            {
                return;
            }

            _index = value;
            _isDirty = true;
        }
    }

    public bool IsDirty()
    {
        return _isDirty;
    }


    public OccludeeDataCSInput GetOccludeeData()
    {
        return _csInput;
    }

    public void SetOcclusionResult(uint result)
    {
        if (isVisible == result)
        {
            return;
        }

        isVisible = result;
        var isEnabled = isVisible != 0;
        foreach (var renderer in renderers)
        {
            renderer.enabled = isEnabled;
        }
    }


    private void updateBounds()
    {
        var isUpdateBounds = false;
        if (_oldPosition != _transform.position)
        {
            isUpdateBounds = true;
            _oldPosition = _transform.position;
        }

        if (isUpdateBounds == false && _oldRotation != _transform.rotation)
        {
            isUpdateBounds = true;
            _oldRotation = _transform.rotation;
        }

        if (isUpdateBounds)
        {
            calculateBounds();
            _isDirty = true;
        }
    }

    private void calculateBounds()
    {
        bounds = renderers[0].bounds;

        for (var i = 1; i < renderers.Count; ++i)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        _csInput = new OccludeeDataCSInput(bounds.center, bounds.extents);
    }
}
