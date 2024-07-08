using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

public class TerrainChunksManager : MonoBehaviour
{
    private const float ViewerMoveThresholdForChunkUpdate = (TerrainChunk.Resolution - 1) * 0.25f;
    private const float SqrViewerMoveThresholdForChunkUpdate = ViewerMoveThresholdForChunkUpdate * ViewerMoveThresholdForChunkUpdate;
    private const int MapTotalChunks = LandscapeManager.MapHeight * LandscapeManager.MapHeight;

    private static int _chunksVisibleInViewDst = 4;
    private static readonly Dictionary<int2, TerrainChunk> TerrainChunksDictionary = new Dictionary<int2, TerrainChunk>();
    private static readonly Dictionary<int2, TerrainChunk> BackupChunksDictionary = new Dictionary<int2, TerrainChunk>();
    private static readonly HashSet<TerrainChunk> TerrainChunksVisibleLastUpdate = new HashSet<TerrainChunk>();
    private static readonly List<TerrainChunk> SurroundTerrainChunks = new List<TerrainChunk>();
    private int2 _lastChunkCoords;
    private int _remainingChunksToCreate = MapTotalChunks;
    private int _backupChunksToCreate;
    private int _visibleMapSize;
    private int _backupBorderSize;

    public static int ChunksVisibleInViewDist => _chunksVisibleInViewDst;
    private static TerrainChunk.LODInfo[] _detailLevels;
    private static int _wrapCountX;
    private static int _wrapCountY;

    public void Initialize()
    {
        _detailLevels = new[] {
            new TerrainChunk.LODInfo(0, 2, false),
            new TerrainChunk.LODInfo(1, 3, true),
            new TerrainChunk.LODInfo(2, 4, false)
        };

        // UpdateVisibleChunks();

        _visibleMapSize = 2 * _chunksVisibleInViewDst + 1;
        _backupBorderSize = _visibleMapSize;
        _remainingChunksToCreate -= _visibleMapSize * _visibleMapSize;

        var currentChunkCoord = Viewer.GetCurrentChunkCoord();
        _lastChunkCoords = new int2(currentChunkCoord.x + (_chunksVisibleInViewDst),
                                    currentChunkCoord.y - (_chunksVisibleInViewDst));
 
        
        _backupChunksToCreate = _backupBorderSize * 4 + 4;

        StartCoroutine(GenerateChunksInBackgroundCoroutine());
    }


    private IEnumerator GenerateChunksInBackgroundCoroutine()
    {
        while (_backupChunksToCreate > 0)
        {

            List<int2> generatedChunkCoords = null;

            var task = Task.Run(() => generatedChunkCoords = GenerateBackupChunkCoords());

            while (!task.IsCompleted)
            {
                yield return null;
            }

            if (task.IsFaulted)
            {
                Debug.LogError("Error generating chunks: " + task.Exception);
                yield break;
            }

            foreach (var coord in generatedChunkCoords)
            {
                var wrappedChunkCoord = GetWrappedChunkCoords(coord);
                var terrainChunk = new TerrainChunk(wrappedChunkCoord, _detailLevels, true);
                terrainChunk.SetChunkCoord(coord);
                terrainChunk.Update();
                BackupChunksDictionary.Add(coord, terrainChunk);
                _backupChunksToCreate--;
            }
        }
    }

    public void Update()
    {
        if (Viewer.PositionChanged())
        {
            Viewer.UpdateOldPosition();
            UpdateVisibleChunks();
        }

        if (Viewer.RotationChanged())
        {
            Viewer.UpdateOldRotation();
            UpdateCulledChunks();
        }
    }

    private void UpdateVisibleChunks()
    {
        foreach (var visibleChunk in TerrainChunksVisibleLastUpdate)
        {
            visibleChunk.SetVisible(false);
        }

        TerrainChunksVisibleLastUpdate.Clear();
        SurroundTerrainChunks.Clear();

        var currentChunkCoord = Viewer.GetCurrentChunkCoord();
        Viewer.ChunkCoord = new int2(currentChunkCoord.x, currentChunkCoord.y);
        UpdateWrapCount(currentChunkCoord.x, currentChunkCoord.y);

        for (int yOffset = -_chunksVisibleInViewDst; yOffset <= _chunksVisibleInViewDst; yOffset++)
        {
            for (int xOffset = -_chunksVisibleInViewDst; xOffset <= _chunksVisibleInViewDst; xOffset++)
            {
                var viewedChunkCoord = new int2(currentChunkCoord.x + xOffset, currentChunkCoord.y + yOffset);
                var wrappedChunkCoord = GetWrappedChunkCoords(viewedChunkCoord);

                TerrainChunk chunk;
                if (TerrainChunksDictionary.TryGetValue(wrappedChunkCoord, out var value))
                {
                    chunk = value;
                    chunk.SetChunkCoord(viewedChunkCoord);
                    chunk.Update();
                    SurroundTerrainChunks.Add(chunk);
                }
                else
                {
                    chunk = new TerrainChunk(wrappedChunkCoord, _detailLevels, false);
                    chunk.SetChunkCoord(viewedChunkCoord);
                    chunk.Update();
                    TerrainChunksDictionary.Add(wrappedChunkCoord, chunk);
                    SurroundTerrainChunks.Add(chunk);
                    _remainingChunksToCreate--;
                }

                if (chunk.IsVisible())
                    TerrainChunksVisibleLastUpdate.Add(chunk);
            }
        }
    }

    private void UpdateWrapCount(int xCoord, int yCoord)
    {
        if (xCoord >= LandscapeManager.MapWidth * _wrapCountX + LandscapeManager.MapWidth)
        {
            _wrapCountX++;
        }
        if (xCoord < -LandscapeManager.MapWidth * _wrapCountX)
        {
            _wrapCountX--;
        }
        if (yCoord >= LandscapeManager.MapHeight * _wrapCountY + LandscapeManager.MapHeight)
        {
            _wrapCountY++;
        }
        if (yCoord < -LandscapeManager.MapHeight * _wrapCountY)
        {
            _wrapCountY--;
        }
    }

    private int2 GetWrappedChunkCoords(int2 viewedChunkCoord)
    {
        int wrappedXCoord = viewedChunkCoord.x;
        int wrappedYCoord = viewedChunkCoord.y;

        if (viewedChunkCoord.x < 0)
        {
            wrappedXCoord = (viewedChunkCoord.x % LandscapeManager.MapWidth + LandscapeManager.MapWidth) % LandscapeManager.MapWidth;
        }
        else if (viewedChunkCoord.x >= LandscapeManager.MapWidth)
        {
            wrappedXCoord = viewedChunkCoord.x % LandscapeManager.MapWidth;
        }

        if (viewedChunkCoord.y < 0)
        {
            wrappedYCoord = (viewedChunkCoord.y % LandscapeManager.MapHeight + LandscapeManager.MapHeight) % LandscapeManager.MapHeight;
        }
        else if (viewedChunkCoord.y >= LandscapeManager.MapHeight)
        {
            wrappedYCoord = viewedChunkCoord.y % LandscapeManager.MapHeight;
        }

        return new int2(wrappedXCoord, wrappedYCoord);
    }

    private void UpdateCulledChunks()
    {
        var viewerForward = Viewer.ForwardV2.normalized;
        foreach (var chunk in SurroundTerrainChunks)
        {
            CullChunkAndSetVisibility(chunk, chunk.IsCulled(viewerForward));
        }
    }

    internal static void CullChunkAndSetVisibility(TerrainChunk chunk, bool isCulled, bool inDistance = true)
    {
        var visible = inDistance;
        if (LandscapeManager.Instance.culling == CullingMode.Layer)
        {
            chunk.GameObject.layer = isCulled ?
                LayerMask.NameToLayer("Culled") :
                LayerMask.NameToLayer("Default");
        }
        else if (LandscapeManager.Instance.culling == CullingMode.Visibility)
        {
            visible = !isCulled && inDistance;
        }

        chunk.SetVisible(visible);
    }

    private List<int2> GenerateBackupChunkCoords()
    {
        List<int2> generatedChunkCoords = new List<int2>();

        int2 coord = _lastChunkCoords + new int2(1, -1);
        int steps = _backupBorderSize;
        _backupBorderSize = 0;

        // to left
        for (int i = 0; i <= steps; i++)
        {
            generatedChunkCoords.Add(coord += new int2(-1, 0));
            _backupBorderSize++;
        }

        // to up
        for (int i = 0; i <= steps; i++)
        {
            generatedChunkCoords.Add(coord += new int2(0, 1));
        }

        // to right
        for (int i = 0; i <= steps; i++)
        {
            generatedChunkCoords.Add(coord += new int2(1, 0));
        }

        // to up
        for (int i = 0; i <= steps; i++)
        {
            generatedChunkCoords.Add(coord += new int2(0, -1));
        }

        _lastChunkCoords = coord;
        _backupBorderSize++; // Se suma 1 porque el primer bucle se queda a 1 de acabar y es donde empieza el siguiente bucle.

        return generatedChunkCoords;
    }
}
