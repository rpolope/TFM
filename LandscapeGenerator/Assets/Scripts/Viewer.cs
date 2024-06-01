using System;
using Unity.Mathematics;
using UnityEngine;

public class Viewer : MonoBehaviour
{
    private Transform _viewerTransform;
    private Vector2 _viewerOldPosition;
    private float _viewerOldRotationY;
    private float _rotationY;
    private int2 _chunkCoord;
    private Vector3 _velocity;
    private Camera _mainCamera;

    public float speed;
    public Vector2 PositionV2 { get; private set; }
    public Vector3 Position;
    public Vector2 ForwardV2 => new (_viewerTransform.forward.x, _viewerTransform.forward.z);
    public static int2 ChunkCoord { get; set; }
    public float FOV => _mainCamera.fieldOfView;

    private void Awake()
    {
        _velocity = new(0, 0, speed);
        _mainCamera = Camera.main;
        _viewerTransform = transform;
        
        var position = _viewerTransform.position;
        var rotation = _viewerTransform.rotation;
        
        _viewerOldPosition = new Vector2(position.x, position.z);
        _viewerOldRotationY = rotation.eulerAngles.y;
        PositionV2 = new Vector2(position.x, position.z);
        _rotationY = rotation.eulerAngles.y;
        Position = _viewerTransform.position;
    }


    private void Update()
    {
        _viewerTransform.position += _velocity * Time.deltaTime;
        PositionV2 = new Vector2(_viewerTransform.position.x, _viewerTransform.position.z) / LandscapeManager.Scale;
        _rotationY = _viewerTransform.rotation.eulerAngles.y;
    }

    public void UpdateOldPosition() => _viewerOldPosition = PositionV2;
    public void UpdateOldRotation() => _viewerOldRotationY = _rotationY;

    public bool PositionChanged() => (_viewerOldPosition - PositionV2).sqrMagnitude > 50f;
                                     // TerrainChunksManager.SqrViewerMoveThresholdForChunkUpdate;


    public bool RotationChanged() => Math.Abs(_rotationY - _viewerOldRotationY) > 5f;
                                     // TerrainChunksManager.ViewerRotateThresholdForChunkUpdate;

}