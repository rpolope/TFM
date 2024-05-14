using System;
using TMPro;
using Unity.Mathematics;
using UnityEngine;

public class Viewer : MonoBehaviour
{
    private Transform _viewerTransform;
    private Vector2 _viewerOldPosition;
    private float _viewerOldRotationY;
    private int2 _chunkCoord;
    private Vector3 _velocity;
    private Camera _mainCamera;

    public float speed;
    public Vector2 PositionV2 { get; private set; }
    public Vector3 PositionV3 => new (PositionV2.x, 0, PositionV2.y);
    public float RotationY { get; private set; }
    public Vector2 ForwardV2 => new (_viewerTransform.forward.x, _viewerTransform.forward.z);
    public int2 ChunkCoord { get; set; }
    public float FOV => _mainCamera.fieldOfView;
    
    void Awake()
    {
        _velocity = new(0, 0, speed);
        _mainCamera = Camera.main;
        _viewerTransform = transform;
        
        var position = _viewerTransform.position;
        var rotation = _viewerTransform.rotation;
        
        _viewerOldPosition = new Vector2(position.x, position.z);
        _viewerOldRotationY = rotation.eulerAngles.y;
        PositionV2 = new Vector2(position.x, position.z);
        RotationY = rotation.eulerAngles.y;
    }
    

    void Update()
    {
        _viewerTransform.position += _velocity * Time.deltaTime;
        PositionV2 = new Vector2(_viewerTransform.position.x, _viewerTransform.position.z);
        RotationY = _viewerTransform.rotation.eulerAngles.y;
    }

    public void UpdateOldPosition() => _viewerOldPosition = PositionV2;
    public void UpdateOldRotation() => _viewerOldRotationY = RotationY;

    public bool PositionChanged() => (_viewerOldPosition - PositionV2).sqrMagnitude > 
                                     LandscapeManager.SqrViewerMoveThresholdForChunkUpdate;
    

    public bool RotationChanged() => Math.Abs(RotationY - _viewerOldRotationY) >
                                     LandscapeManager.ViewerRotateThresholdForChunkUpdate;
    
}