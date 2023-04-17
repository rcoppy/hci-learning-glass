using System;
using System.Collections;
using System.Collections.Generic;
using nuitrack;
using UnityEngine;
using NuitrackSDK;

public class PoseTrackingAvatar : MonoBehaviour
{
    [SerializeField] private GameObject _jointPrefab; 
    
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

    private void Awake()
    {
        _createdJoints = new Dictionary<JointType, GameObject>();
        
        foreach (var j in _allJoints) {
            _createdJoints[j] = Instantiate(_jointPrefab);
            _createdJoints[j].transform.SetParent(transform);
        }
    }

    void Update()
    {
        if (NuitrackManager.Users.Current != null && NuitrackManager.Users.Current.Skeleton != null)
        {
            foreach (var j in _allJoints)
            {
                UserData.SkeletonData.Joint joint = NuitrackManager.Users.Current.Skeleton.GetJoint(j);
                _createdJoints[j].transform.localPosition = joint.Position;    
            }
        }
    }
}
