using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(LineRenderer))]
public class TracePointer : MonoBehaviour
{
    private LineRenderer _renderer;
    private Vector3 _lastPointPosition = Vector3.up;
    private float _lastPointSpawnTime = 0f;
    private LinkedList<Vector3> _points;
    private float _lastDespawnTime = 0f;
    private bool _isEnabled = false; 

    [SerializeField] private Camera _camera;
    [SerializeField] private float _distDeltaThreshold = 0.01f;
    [SerializeField] private float _timeDeltaThreshold = 1 / 10f;
    [SerializeField] private float _timeToWaitForDespawn = 0.3f;
    [SerializeField] private int _maxPoints = 40; 

    private void Awake()
    {
        _renderer = GetComponent<LineRenderer>();
        _points = new LinkedList<Vector3>(); // use as a double-ended queue 
        
        _renderer.SetPositions(_points.ToArray());
    }

    private void Update()
    {
        if (Time.time - _lastDespawnTime < _timeToWaitForDespawn) return; 
        
        _lastDespawnTime = Time.time; 
        DespawnOldestPoint();
        
    }

    public void ToggleIsEnabled()
    {
        SetIsEnabled(!_isEnabled);
    }

    public void HandleToggle(InputAction.CallbackContext context)
    {
        if (!context.performed) return; 
        
        ToggleIsEnabled();
    }

    public void SetIsEnabled(bool flag)
    {
        if (_isEnabled && flag) return;
        if (!_isEnabled && !flag) return;

        if (flag)
        {
            _points = new LinkedList<Vector3>();
            _renderer.positionCount = 0;
        }

        _isEnabled = flag; 
    }

    public void HandlePointer(InputAction.CallbackContext context)
    {
        if (!context.performed || !_isEnabled) return;

        var pointer = context.ReadValue<Vector2>();
        var viewportPoint = _camera.ScreenToViewportPoint(
            new Vector3(pointer.x, pointer.y, _camera.nearClipPlane)
        );

        var worldPoint = GetWorldPositionFromViewportPoint(viewportPoint);
        
        if (worldPoint == null) return;

        float distDelta = ((Vector3)worldPoint - _lastPointPosition).magnitude; 
        if (distDelta < _distDeltaThreshold) return;

        float timeDelta = Time.time - _lastPointSpawnTime;
        if (timeDelta < _timeDeltaThreshold) return;

        SpawnTracePoint((Vector3)worldPoint);
    }

    void DespawnOldestPoint()
    {
        if (_points.Count <= 0) return;
        
        _points.RemoveFirst();
        _renderer.positionCount = _points.Count;
        _renderer.SetPositions(_points.ToArray());
    }

    void SpawnTracePoint(Vector3 pos)
    {
        _lastPointPosition = pos;
        _lastPointSpawnTime = Time.time;

        _points.AddLast(pos); 
        
        if (_points.Count > _maxPoints) _points.RemoveFirst();
        
        _renderer.positionCount = _points.Count;
        _renderer.SetPositions(_points.ToArray());
    }

    Vector3? GetWorldPositionFromViewportPoint(Vector3 viewportPoint)
    {
        var ray = _camera.ViewportPointToRay(viewportPoint);

        if (Physics.Raycast(ray, out var hit, _camera.farClipPlane)) return hit.point + 0.06f * hit.normal;
        
        var planePoint = _lastPointPosition;
        var planeNormal = (_camera.transform.position - planePoint).normalized; 
        var start = ray.origin;
        var end = ray.GetPoint(150);

        return GetIntersectionPointOfPlaneAndLine(planeNormal, planePoint, start, end);
    }

    Vector3? GetIntersectionPointOfPlaneAndLine(Vector3 planeNormal, Vector3 pointOnPlane, Vector3 lineStart,
        Vector3 lineEnd)
    {
        var n = planeNormal.normalized; 
        
        /*float a, b, c;
            
        a = lineEnd.x;
        b = lineEnd.y;
        c = lineEnd.z;

        float x, y, z;
        x = n.x;
        y = n.y;
        z = n.z; 

        float u, v, w;
        u = lineStart.x;
        v = lineStart.y;
        w = lineStart.z;

        float d, e, f;
        d = pointOnPlane.x;
        e = pointOnPlane.y;
        f = pointOnPlane.z;*/

        
        float lower = Vector3.Dot(lineEnd, n) - Vector3.Dot(lineStart, n);

        if (Mathf.Approximately(lower, 0f)) return null;
        
        float upper = Vector3.Dot(pointOnPlane, n) - Vector3.Dot(lineStart, n);

        float t = upper / lower; 
        
        return t * (lineEnd - lineStart) + lineStart;
    }
}
