using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using nuitrack;
using UnityEngine;
using NuitrackSDK;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PoseTrackingAvatar : MonoBehaviour
{
    [SerializeField] private int _velocityBufferSize = 40; 
    [SerializeField] private GameObject _jointPrefab;
    [SerializeField] private UnityEvent<Vector2> _onHandMove; 
    
    [SerializeField] private UnityEvent<Vector2> _onHandUpdatePosition; 
    
    public bool IsDraggingEnabled = false;
    
    public void SetIsDraggingEnabled(bool flag) {
    	IsDraggingEnabled = flag;
    }
    
    private JointType[] _leftJoints =
    {
        JointType.LeftElbow, JointType.LeftHand, JointType.LeftShoulder, JointType.LeftWrist
    }; 
    
    private JointType[] _rightJoints =
    {
        JointType.RightElbow, JointType.RightHand, JointType.RightShoulder, JointType.RightWrist
    };
    
    private JointType[] _allJoints = {
        JointType.LeftElbow, JointType.LeftHand, JointType.LeftShoulder, JointType.LeftWrist, 
        JointType.RightElbow, JointType.RightHand, JointType.RightShoulder, JointType.RightWrist
    };

    private Dictionary<JointType, GameObject> _createdJoints;
    private UnityEngine.Vector2 _lastProjectedHandPosition = UnityEngine.Vector3.zero;

    private Queue<UnityEngine.Vector2> _velocityBuffer; 
    private Queue<UnityEngine.Vector2> _positionBuffer; 
    

    private void Awake()
    {
        _velocityBuffer = new Queue<Vector2>();
        _positionBuffer = new Queue<Vector2>();
        
        _createdJoints = new Dictionary<JointType, GameObject>();
        
        foreach (var j in _allJoints) {
            _createdJoints[j] = Instantiate(_jointPrefab);
            _createdJoints[j].transform.SetParent(transform);
        }
    }

    void Update()
    {
        if (NuitrackManager.Users.Current == null) return;
        
        if (NuitrackManager.Users.Current.Skeleton != null)
        {
            foreach (var j in _allJoints)
            {
                UserData.SkeletonData.Joint joint = NuitrackManager.Users.Current.Skeleton.GetJoint(j);
                _createdJoints[j].transform.localPosition = joint.Position;    
            }
        }

        if (NuitrackManager.Users.Current.RightHand != null)
        {
            var delta = NuitrackManager.Users.Current.RightHand.Proj - _lastProjectedHandPosition;
            if (IsDraggingEnabled) {
            	_velocityBuffer.Enqueue(delta / Time.deltaTime);
            } else {
            	_velocityBuffer.Enqueue(UnityEngine.Vector2.zero); 
            }
            
            _positionBuffer.Enqueue(NuitrackManager.Users.Current.RightHand.Proj);
            
            if (_velocityBuffer.Count > _velocityBufferSize) _velocityBuffer.Dequeue();
            if (_positionBuffer.Count > _velocityBufferSize) _positionBuffer.Dequeue();

            var movingVelocityAverage = 1f / _velocityBufferSize * _velocityBuffer.Aggregate(UnityEngine.Vector2.zero, (total, next) => total += next);
            
            var movingPositionAverage = 1f / _velocityBufferSize * _positionBuffer.Aggregate(UnityEngine.Vector2.zero, (total, next) => total += next); 
            
            _onHandMove?.Invoke(Time.deltaTime * movingVelocityAverage);
            
            _onHandUpdatePosition?.Invoke(movingPositionAverage);
        }
    }

    private void LateUpdate()
    {
        if (NuitrackManager.Users.Current == null) return;
        if (NuitrackManager.Users.Current.RightHand != null)
        {
            _lastProjectedHandPosition = NuitrackManager.Users.Current.RightHand.Proj;
        }
    }
}
