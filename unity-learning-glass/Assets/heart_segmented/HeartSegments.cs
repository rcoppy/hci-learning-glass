using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeartSegments : MonoBehaviour
{
    public enum SinusSegment
    {
        front, 
        back, 
        both
    }

    private Queue<SinusSegment> _segmentQueue;
    private SinusSegment _currentSegment = SinusSegment.both;

    [SerializeField] private GameObject _sinusFront; 
    [SerializeField] private GameObject _sinusBack; 
    
    [SerializeField] private GameObject _vasculature;
    [SerializeField] private GameObject _loop;
    
    // Start is called before the first frame update
    void Start()
    {
        _segmentQueue = new Queue<SinusSegment>(); 
        
        _segmentQueue.Enqueue(SinusSegment.back); 
        _segmentQueue.Enqueue(SinusSegment.both);
        _segmentQueue.Enqueue(SinusSegment.front);
        
        _sinusBack.SetActive(true);
        _sinusFront.SetActive(true);

        _vasculature.SetActive(true);
        _loop.SetActive(false);
    }

    public void CycleSegment()
    {
        var s = _segmentQueue.Dequeue(); 
        _segmentQueue.Enqueue(s);

        switch (s)
        {
            case SinusSegment.back: 
                _sinusBack.SetActive(true);
                _sinusFront.SetActive(false);
                break;
            case SinusSegment.front: 
                _sinusBack.SetActive(false);
                _sinusFront.SetActive(true);
                break;
            case SinusSegment.both: 
                _sinusBack.SetActive(true);
                _sinusFront.SetActive(true);
                break;
        }
    }

    public void ToggleVasculature()
    {
        _vasculature.SetActive(!_vasculature.activeSelf);
    }
    
    public void ToggleLoop()
    {
        _loop.SetActive(!_loop.activeSelf);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
