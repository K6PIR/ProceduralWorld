using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour {
    
    #region Public Variables

    public int colliderLODIndex;
    public LODInfo[] detailLevels;

    public MeshSettings meshSettings;
    public HeightMapSettings heightMapSettings;
    public TextureData textureSettings;
    public ForestSettings forestSettings;

    public Transform viewer;

    public Material mapMaterial;

    #endregion

    #region Private Variables
    
    private const float MoveThresholdForChunkUpdate = 25f;
    private const float SqrMoveThresholdForChunkUpdate = MoveThresholdForChunkUpdate * MoveThresholdForChunkUpdate;

    private Vector2 _viewerPosition;
    private Vector2 _viewerPositionOld;
    
    private float _meshWorldSize;
    private int _chunksVisibleInViewDistance;

    private Forest _forest = new Forest();
    
    private Dictionary<Vector2, TerrainChunk> _terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    private List<TerrainChunk> _visibleTerrainChunks = new List<TerrainChunk>();
    
    #endregion

    #region MonoBehaviour Callback
    private void Start() {
        
        textureSettings.ApplyToMaterial(mapMaterial);
        textureSettings.UpdateMeshHeights(mapMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);

        float maxViewDistance = detailLevels[detailLevels.Length - 1].visibleDstThreshold;
        _meshWorldSize = meshSettings.meshWorldSize;
        _chunksVisibleInViewDistance = Mathf.RoundToInt(maxViewDistance / _meshWorldSize);
        
        UpdateVisibleChunks();

        SpawnViewer();
    }

    void Update() {
        _viewerPosition = new Vector2(viewer.position.x, viewer.position.z);

        if (_viewerPosition != _viewerPositionOld) {
            foreach (TerrainChunk chunk in _visibleTerrainChunks) {
                chunk.UpdateCollisionMesh();
            }
        }

        if ((_viewerPositionOld - _viewerPosition).sqrMagnitude > SqrMoveThresholdForChunkUpdate) {
            _viewerPositionOld = _viewerPosition;
            UpdateVisibleChunks();
        }
    }
    
    #endregion
    
    #region Public Methods

    public List<TerrainChunk> GetVisibleTerrainChunks() {
        return _visibleTerrainChunks;
    }
    
    #endregion
    
    #region Private Methods
    
    void UpdateVisibleChunks() {
        HashSet<Vector2> alreadyUpdatedChunkCoords = new HashSet<Vector2>();
        for (int i = _visibleTerrainChunks.Count-1;  i >= 0 ; i--) {
            alreadyUpdatedChunkCoords.Add(_visibleTerrainChunks[i].coord);
            _visibleTerrainChunks[i].Update();
        }

        int currentChunkCoordX = Mathf.RoundToInt(_viewerPosition.x / _meshWorldSize);
        int currentChunkCoordY = Mathf.RoundToInt(_viewerPosition.y / _meshWorldSize);
        
        for (int yOffset = -_chunksVisibleInViewDistance; yOffset <= _chunksVisibleInViewDistance; yOffset++) {
            for (int xOffset = -_chunksVisibleInViewDistance; xOffset <= _chunksVisibleInViewDistance; xOffset++) {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if (alreadyUpdatedChunkCoords.Contains(viewedChunkCoord)) continue;
                
                if (_terrainChunkDictionary.ContainsKey(viewedChunkCoord)) {
                    _terrainChunkDictionary[viewedChunkCoord].Update();
                } else {
                    TerrainChunk newTerrainChunk = new TerrainChunk(viewedChunkCoord, heightMapSettings, meshSettings, _forest, forestSettings, detailLevels,
                        colliderLODIndex, transform, viewer, mapMaterial);
                    _terrainChunkDictionary.Add(viewedChunkCoord, newTerrainChunk);
                    newTerrainChunk.onVisibilityChanged += OnTerrainChunkVisibilityChanged;
                    newTerrainChunk.Load();
                }
            }
        }
    }

    void OnTerrainChunkVisibilityChanged(TerrainChunk chunk, bool isVisible) {
        if (isVisible) {
            _visibleTerrainChunks.Add(chunk);
        } else {
            _visibleTerrainChunks.Remove(chunk);
        }
    }

    void SpawnViewer() {
        Vector2Int viewerChunkCoord = new Vector2Int(
            Mathf.RoundToInt(_viewerPosition.x / _meshWorldSize),
            Mathf.RoundToInt(_viewerPosition.y / _meshWorldSize)
            );

        _terrainChunkDictionary[viewerChunkCoord].SpawnViewer();
    }

    #endregion
}

[System.Serializable]
public struct LODInfo {
    [Range(0, MeshSettings.numSupportedLODs - 1)]
    public int lod;

    public float visibleDstThreshold;

    public float sqrVisibleDstThreshold { get { return visibleDstThreshold * visibleDstThreshold; } }
}