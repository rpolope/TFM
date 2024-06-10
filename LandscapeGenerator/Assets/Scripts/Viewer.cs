using System;
using Unity.Mathematics;
using UnityEngine;

public class Viewer : MonoBehaviour
{
    private static Transform _transform;
    private static Vector2 _viewerOldPosition;
    private static Camera _mainCamera;

    private static float _viewerOldRotationY;
    private static float _rotationY;
    private int2 _chunkCoord;
    private Vector3 _velocity;

    public static Vector2 PositionV2 => new (_transform.position.x, _transform.position.z);
    public static Vector2 ForwardV2 => new (_transform.forward.x, _transform.forward.z);
    public static int2 ChunkCoord { get; set; }
    public static float FOV => _mainCamera.fieldOfView;
    
    public float speed;


    private void Awake()
    {
        _velocity = new(0, 0, speed);
        _mainCamera = Camera.main;
        _transform = transform;

        var rotation = _transform.rotation;
        _viewerOldRotationY = rotation.eulerAngles.y;
        _rotationY = rotation.eulerAngles.y;
    }


    private void Update()
    {
        _transform.position += _velocity * Time.deltaTime;
        _rotationY = _transform.rotation.eulerAngles.y;
    }

    public static void UpdateOldPosition() => _viewerOldPosition = PositionV2;
    public static void UpdateOldRotation() => _viewerOldRotationY = _rotationY;

    public static bool PositionChanged() => (_viewerOldPosition - PositionV2).sqrMagnitude > 50f;
                                     // TerrainChunksManager.SqrViewerMoveThresholdForChunkUpdate;

    public static bool RotationChanged() => Math.Abs(_rotationY - _viewerOldRotationY) > 5f;
                                     // TerrainChunksManager.ViewerRotateThresholdForChunkUpdate;
    
    public static void SetInitialPos(float2 position)
    {
        _transform.position = new Vector3(
            position.x,
            _transform.position.y,
            position.y
        );
        _viewerOldPosition = new Vector2(position.x, position.y);
    }
}