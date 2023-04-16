using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleFollowCamera : MonoBehaviour
{
    [SerializeField] Transform _target;
    [SerializeField] private float _yawAngle = 0f;
    [SerializeField] private float _pitchAngle = 0f;
    [SerializeField] private float _yawSensitivity = 1f;
    [SerializeField] private float _pitchSensitivity = 1f;
    [SerializeField] private bool _invertYawSteering = false;
    [SerializeField] private bool _invertPitchSteering = false;
    [SerializeField] private float _locationSnapCoefficient = 4f;
    [SerializeField] private float _rotationSnapCoefficient = 4f;
    [SerializeField] private float _occlusionCheckOffset = 2f;
    [SerializeField] private LayerMask _occluderLayer;
    [SerializeField] private bool _useLocalUpAxis = false;
    [SerializeField] private float _lookRotationBlendFactor = 0.4f;
    [SerializeField] private Quaternion _originalForwardRotation;
    [SerializeField] private float _maxPitchAngle = 80f;
    [SerializeField] private float _minPitchAngle = -80f;
    [SerializeField] private float _zoomLevel = 1f; 
    [SerializeField] private float _zoomCoefficient = 0.1f;
    [SerializeField] private bool _invertedZoom = true; 
    
    private Vector3 _offset;
    private Vector3 _originalForward;
    private Quaternion _originalRotationDifference;
    private Quaternion _originalSelfRotation;
    private Quaternion _originalTargetRotation;
    private float _yawAxis = 0f; 
    private float _pitchAxis = 0f;
    private float _zoomDelta = 0f; 
    
    LayerMask _trackLayer;

    private Queue<bool> _occlusionTracker; 

    // Start is called before the first frame update
    void Start()
    {
        _trackLayer = _occluderLayer; // LayerMask.GetMask(_occluderLayerName);
        _offset = transform.position - _target.position;
        _originalForward = _target.forward;
        _originalTargetRotation = _target.rotation;
        _originalSelfRotation = transform.rotation;
        _originalRotationDifference = transform.rotation * Quaternion.Inverse(_target.rotation); // Quaternion.FromToRotation(_target.forward, transform.forward);
        _originalForwardRotation = Quaternion.FromToRotation(_target.forward, transform.forward);
        _occlusionTracker = new Queue<bool>(); 
    }

    enum OcclusionStatus
    {
        Partial, 
        Hidden, 
        Visible
    }

    OcclusionStatus GetIsTargetVisible()
    {
        var origin = transform.position + -1 * _occlusionCheckOffset * transform.up; 
        var delta1 = _target.position - origin;
        var delta2 = _target.position + -0.25f * _target.up - origin; 
        
        Debug.DrawLine(origin, origin + delta1);
        Debug.DrawLine(origin, origin + delta2);
        
        var primary = !Physics.Raycast(origin, delta1.normalized, delta1.magnitude, _trackLayer);
        var secondary = !Physics.Raycast(origin, delta2.normalized, delta2.magnitude, _trackLayer);

        if (primary && secondary) return OcclusionStatus.Visible;
        if (!primary && !secondary) return OcclusionStatus.Hidden;
        return OcclusionStatus.Partial; 
    }
    
    // Update is called once per frame
    void FixedUpdate()
    {
        _zoomLevel += (_invertedZoom ? -1 : 1) * _zoomDelta; 
        _zoomLevel = Mathf.Clamp(_zoomLevel, 0.1f, 2.5f);
        
        _pitchAngle += (_invertPitchSteering ? -1f : 1f) * 180f * _pitchSensitivity * Time.fixedDeltaTime * _pitchAxis;
        _yawAngle += (_invertYawSteering ? -1f : 1f) * 180f * _yawSensitivity * Time.fixedDeltaTime * _yawAxis;
        // _yawAngle *= 0.995f;
        var yawRot = Quaternion.AngleAxis(_yawAngle, _target.up);

        var visible = GetIsTargetVisible(); 
        
        /*_occlusionTracker.Enqueue(visible);
        if (_occlusionTracker.Count > 120)
        {
            _occlusionTracker.Dequeue(); 
        }*/
        
        var pitchDir = visible == OcclusionStatus.Hidden ? 1f : 0f; 
        _pitchAngle += 90f * Time.fixedDeltaTime * pitchDir;
        _pitchAngle = Mathf.Clamp(_pitchAngle, _minPitchAngle, _maxPitchAngle);
        
        /* int count = 0; 
        foreach (bool val in _occlusionTracker.ToArray())
        {
            if (val)
            {
                count++; 
            }
        }

        bool safeToReset = count > 80;  */
        
        if (visible == OcclusionStatus.Visible)
        {
            // _pitchAngle *= 0.99f; 
        }
        else
        {
            // Debug.Log(visible);
        }
        
        /*transform.position = Vector3.Lerp(transform.position,
            _target.position + yawRot * SimpleFollowCamera.FixedFromToRotation(_originalForward.normalized, _target.forward.normalized, Quaternion.AngleAxis(180f, _target.up))  * _offset,
            Time.fixedDeltaTime * _locationSnapCoefficient);*/

        var dif = _target.rotation * Quaternion.Inverse(_originalTargetRotation);
        
        transform.position = Vector3.Lerp(transform.position,
            _target.position + yawRot * dif * (_zoomLevel * _offset),
            Time.fixedDeltaTime * _locationSnapCoefficient);

        var delta = transform.position - _target.position; 
        var pitchAxis = Vector3.Cross( delta, _useLocalUpAxis ? _target.up : Vector3.up).normalized;
        var pitchRot = Quaternion.AngleAxis(_pitchAngle, pitchAxis);
        
        transform.position = Vector3.Lerp(transform.position,
            _target.position + pitchRot * delta,
            Time.fixedDeltaTime * _locationSnapCoefficient);
        
        // var worldRot = Quaternion.LookRotation(Vector3.forward, Vector3.up);

        var upFactor = 1f; // Vector3.Dot(Vector3.up, transform.up) < 0f ? -1f : 1f;
        var up = _useLocalUpAxis ? upFactor * _target.up : upFactor * Vector3.up;
        
        // var localRot = pitchRot * yawRot * Quaternion.LookRotation( _originalForwardRotation * _target.forward, up);

        var localRot = pitchRot * yawRot * dif * _originalSelfRotation; 
        var clampedRot = Quaternion.LookRotation(_target.position - transform.position, up);
        var targetRot = Quaternion.Slerp(localRot, clampedRot, _lookRotationBlendFactor);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.fixedDeltaTime * _rotationSnapCoefficient);

        // transform.rotation *= Quaternion.FromToRotation(transform.up, Vector3.up); 
    }

    public void UpdateYawAxis(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
             _yawAxis = context.action.ReadValue<Vector2>().x;
             _pitchAxis = context.action.ReadValue<Vector2>().y;
        } else if (context.canceled)
        {
            _yawAxis = 0f;
            _pitchAxis = 0f; 
        }
    }

    public void UpdateZoom(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            _zoomDelta = _zoomCoefficient * context.action.ReadValue<Vector2>().y;
        } else if (context.canceled)
        {
            _zoomDelta = 0f; 
        }
    }
    
    public void SetYawAngle(float angle)
    {
        _yawAngle = angle; 
    }
    
    // https://forum.unity.com/threads/fix-inside-quaternion-fromtorotation-discontinuity.706514/
    // default value is optional
    static public Quaternion FixedFromToRotation(Vector3 dir1, Vector3 dir2,
        Quaternion whenOppositeVectors = default(Quaternion)) {
     
        float r = 1f + Vector3.Dot(dir1, dir2);
     
        if(r < 1E-6f) {
            if(whenOppositeVectors == default(Quaternion)) {
                // simply get the default behavior
                return Quaternion.FromToRotation(dir1, dir2);
            }
            return whenOppositeVectors;
        }
     
        Vector3 w = Vector3.Cross(dir1, dir2);
        return new Quaternion(w.x, w.y, w.z, r).normalized;
    }

}
