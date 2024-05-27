using System;
using Unity.Mathematics;
using UnityEngine;

public class Viewer : MonoBehaviour
{
    private static Camera _mainCamera;

    private static Transform _viewerTransform;
    private static Vector2 _viewerOldPosition;
    private static float _viewerOldRotationY;
    private static float _rotationY;
    private int2 _chunkCoord;
    private Vector3 _velocity;

    public float speed;
    public static Vector2Int InitialCoords { get; set; }
    public static Vector2 PositionV2 { get; private set; }
    public static Vector2 ForwardV2 => new (_viewerTransform.forward.x, _viewerTransform.forward.z);
    public static Coordinates ChunkCoord { get; set; }
    public static float FOV => _mainCamera.fieldOfView;

    private void Awake()
    {
        _mainCamera = Camera.main;
        _velocity = new Vector3(0, 0, speed);
        _viewerTransform = transform;
    }

    public static void Initialize()
    {
        var position = _viewerTransform.position;
        var rotation = _viewerTransform.rotation;
        var initialPos = TerrainChunksManager.GetChunkFromCoordinates(new Coordinates(InitialCoords.x, InitialCoords.y)).Position;
        _viewerTransform.position = new Vector3(initialPos.x, 0, initialPos.y);
        _viewerOldPosition = new Vector2(position.x, position.z);
        _viewerOldRotationY = rotation.eulerAngles.y;
        PositionV2 = new Vector2(position.x, position.z);
        _rotationY = rotation.eulerAngles.y;
    }

    private void Update()
    {
        var position = _viewerTransform.position;
        position += _velocity * Time.deltaTime;
        _viewerTransform.position = position;
        PositionV2 = new Vector2(position.x, position.z) / LandscapeManager.Scale;
        _rotationY = _viewerTransform.rotation.eulerAngles.y;
    }

    public static void UpdateOldPosition() => _viewerOldPosition = PositionV2;
    public static void UpdateOldRotation() => _viewerOldRotationY = _rotationY;

    public static bool PositionChanged() => (_viewerOldPosition - PositionV2).sqrMagnitude > 50f;
                                     // LandscapeManager.SqrViewerMoveThresholdForChunkUpdate;


    public static bool RotationChanged() => Math.Abs(_rotationY - _viewerOldRotationY) > 5f;
                                     // LandscapeManager.ViewerRotateThresholdForChunkUpdate;

}