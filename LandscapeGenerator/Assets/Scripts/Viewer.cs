using System;
using Unity.Mathematics;
using UnityEngine;
using static TerrainChunksManager;

public class Viewer : MonoBehaviour
{
    private static Transform _transform;
    private static Vector2 _viewerOldPosition;
    private static Camera _mainCamera;

    private static float _viewerOldRotationY;
    private static float _rotationY;
    private int2 _chunkCoord;
    private static Vector3 _velocity;

    public static Vector2 PositionV2 => new (_transform.position.x, _transform.position.z);
    public static Vector2 ForwardV2 => new (_transform.forward.x, _transform.forward.z);
    public static int2 ChunkCoord { get; set; }
    public static float FOV => _mainCamera.fieldOfView;
    public static float ExtendedFOV => 100f;
    public float speed = 200f;

    private static float _speed;


    private void Awake()
    {
        _speed = speed;
        _mainCamera = Camera.main;
        _transform = transform;
        _velocity = _transform.forward.normalized * speed;
        
        var rotation = _transform.rotation;
        _viewerOldRotationY = rotation.eulerAngles.y;
        _rotationY = rotation.eulerAngles.y;
    }


    private void Update()
    {
        _velocity = _transform.forward.normalized * speed;
        _transform.position += _velocity * Time.deltaTime;
        _rotationY = _transform.rotation.eulerAngles.y;
    }

    public static void UpdateOldPosition() => _viewerOldPosition = PositionV2;
    public static void UpdateOldRotation() => _viewerOldRotationY = _rotationY;

    public static bool PositionChanged() => (_viewerOldPosition - PositionV2).sqrMagnitude > 
                                     SqrViewerMoveThresholdForChunkUpdate;

    public static bool RotationChanged()
    {
        _velocity = _transform.forward.normalized * _speed;
        return Math.Abs(_rotationY - _viewerOldRotationY) >
               ViewerRotateThresholdForChunkUpdate;
    }
    
    public static void SetInitialPos(float2 position)
    {
        _transform.position = new Vector3(
            position.x,
            _transform.position.y,
            position.y
        );
        _viewerOldPosition = new Vector2(position.x, position.y);
    }

    public static void UpdateChunkCoord()
    {
        float offset = TerrainChunk.WorldSize * 0.5f;

        int currentChunkCoordX = (int)((PositionV2.x + offset) / TerrainChunk.WorldSize);
        int currentChunkCoordY = (int)((PositionV2.y + offset) / TerrainChunk.WorldSize);
        
        ChunkCoord = new int2(currentChunkCoordX, currentChunkCoordY);
    }
}