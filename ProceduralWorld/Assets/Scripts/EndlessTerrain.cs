using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour {
    
    #region Public Variables

    public int colliderLODIndex;
    public LODInfo[] detailLevels;
    public static float maxViewDistance;
    
    public Transform viewer;

    public static Vector2 viewerPosition;
    
    public Material mapMaterial;

    #endregion

    #region Private Variables
    
    private const float MoveThresholdForChunkUpdate = 25f;
    private const float SqrMoveThresholdForChunkUpdate = MoveThresholdForChunkUpdate * MoveThresholdForChunkUpdate;
    private const float ColliderGenerationDistanceThreshold = 5;

    private Vector2 _viewerPositionOld;
    
    private static MapGenerator _mapGenerator;
    
    private int _chunkSize;
    private int _chunksVisibleInViewDistance;

    private Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    private static List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();
    
    #endregion

    #region MonoBehaviour Callback
    private void Start() {
        _mapGenerator = FindObjectOfType<MapGenerator>();
        
        maxViewDistance = detailLevels[detailLevels.Length - 1].visibleDstThreshold;
        _chunkSize = _mapGenerator.MapChunkSize - 1;
        _chunksVisibleInViewDistance = Mathf.RoundToInt(maxViewDistance / _chunkSize);
        
        UpdateVisibleChunks();
    }

    void Update() {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z) / _mapGenerator.terrainData.uniformScale;

        if (viewerPosition != _viewerPositionOld) {
            foreach (TerrainChunk chunk in visibleTerrainChunks) {
                chunk.UpdateCollisionMesh();
            }
        }

        if ((_viewerPositionOld - viewerPosition).sqrMagnitude > SqrMoveThresholdForChunkUpdate) {
            _viewerPositionOld = viewerPosition;
            UpdateVisibleChunks();
        }
    }
    
    #endregion

    #region Private Methods
    
    void UpdateVisibleChunks() {
        HashSet<Vector2> alreadyUpdatedChunkCoords = new HashSet<Vector2>();
        for (int i = visibleTerrainChunks.Count-1;  i >= 0 ; i--) {
            alreadyUpdatedChunkCoords.Add(visibleTerrainChunks[i].coord);
            visibleTerrainChunks[i].Update();
        }

        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / _chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / _chunkSize);
        
        for (int yOffset = -_chunksVisibleInViewDistance; yOffset <= _chunksVisibleInViewDistance; yOffset++) {
            for (int xOffset = -_chunksVisibleInViewDistance; xOffset <= _chunksVisibleInViewDistance; xOffset++) {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if (alreadyUpdatedChunkCoords.Contains(viewedChunkCoord)) continue;
                
                if (terrainChunkDictionary.ContainsKey(viewedChunkCoord)) {
                    terrainChunkDictionary[viewedChunkCoord].Update();
                } else {
                    terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, _chunkSize, detailLevels, colliderLODIndex, transform, mapMaterial));
                }
            }
        }
    }

    #endregion
    
    #region Inner Class
    public class TerrainChunk {

        public Vector2 coord;
        
        private GameObject _meshObject;
        private Vector2 _position;
        private Bounds _bounds;
        
        private MeshRenderer _meshRenderer;
        private MeshFilter _meshFilter;
        private MeshCollider _meshCollider;

        private LODInfo[] _detailLevels;
        private LODMesh[] _lodMeshes;
        private int _colliderLODIndex;
        private MapData _mapData;
        private bool _mapDataReceived;
        private int _previousLODIndex = -1;
        private bool _hasSetCollider;
        
        public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, int colliderLODIndex, Transform parent, Material material) {
            this.coord = coord;
            _detailLevels = detailLevels;
            _colliderLODIndex = colliderLODIndex;
            
            _position = coord * size;
            _bounds = new Bounds(_position, Vector3.one * size);
            Vector3 positionV3 = new Vector3(_position.x, 0, _position.y);

            _meshObject = new GameObject("TerrainChunk");
            _meshObject.transform.position = positionV3 * _mapGenerator.terrainData.uniformScale;
            _meshObject.transform.parent = parent;
            _meshObject.transform.localScale = Vector3.one *  _mapGenerator.terrainData.uniformScale;

            _meshRenderer = _meshObject.AddComponent<MeshRenderer>();
            _meshRenderer.material = material;
            
            _meshFilter = _meshObject.AddComponent<MeshFilter>();

            _meshCollider = _meshObject.AddComponent<MeshCollider>();
            
            setVisible(false);
            
            _lodMeshes = new LODMesh[detailLevels.Length];
            for (int i = 0; i < detailLevels.Length; i++) {
                _lodMeshes[i] = new LODMesh(_detailLevels[i].lod);
                _lodMeshes[i].updateCallback += Update;
                if (i == colliderLODIndex) {
                    _lodMeshes[i].updateCallback += UpdateCollisionMesh;
                }
            }
            
            _mapGenerator.RequestMapData(_position, OnMapDataReceived);
        }

        void OnMapDataReceived(MapData mapData) {
            _mapData = mapData;
            _mapDataReceived = true;
            
            Update();
        }

        public void Update() {
            if (!_mapDataReceived) return;

            float distanceFromNearestEdge = Mathf.Sqrt(_bounds.SqrDistance(viewerPosition));

            bool wasVisible = IsVisible();
            bool visible = distanceFromNearestEdge <= maxViewDistance;

            if (visible) {
                int lodIndex = 0;

                for (int i = 0; i < _detailLevels.Length - 1; i++) {
                    if (distanceFromNearestEdge > _detailLevels[i].visibleDstThreshold) {
                        lodIndex = i + 1;
                    } else {
                        break;
                    }
                }

                if (lodIndex != _previousLODIndex) {
                    LODMesh lodMesh = _lodMeshes[lodIndex];
                    if (lodMesh.hasMesh) {
                        _previousLODIndex = lodIndex;
                        _meshFilter.mesh = lodMesh.mesh;
                    } else if ( !lodMesh.hasRequestedMesh) {
                        lodMesh.RequestMesh(_mapData);
                    }
                }
            }
            
            if (wasVisible != visible) {
                if (visible)
                    visibleTerrainChunks.Add(this);
                else
                    visibleTerrainChunks.Remove(this);
            }
            
            setVisible(visible);
        }

        public void UpdateCollisionMesh() {
            if (_hasSetCollider) return;
            
            float sqrDstFromViewerToEdge = _bounds.SqrDistance(viewerPosition);

            if (sqrDstFromViewerToEdge < _detailLevels[_colliderLODIndex].sqrVisibleDstThreshold) {
                if (!_lodMeshes[_colliderLODIndex].hasRequestedMesh) {
                    _lodMeshes[_colliderLODIndex].RequestMesh(_mapData);
                }
            }
            
            if (sqrDstFromViewerToEdge < ColliderGenerationDistanceThreshold * ColliderGenerationDistanceThreshold) {
                if (_lodMeshes[_colliderLODIndex].hasMesh) {
                    _meshCollider.sharedMesh = _lodMeshes[_colliderLODIndex].mesh;
                    _hasSetCollider = true;
                }
            }
        }
        
        public void setVisible(bool visible) {
            _meshObject.SetActive(visible);
        }

        public bool IsVisible() {
            return _meshObject.activeSelf;
        }
    }

    class LODMesh {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        private int lod;

        public event System.Action updateCallback;
        
        public LODMesh(int lod) {
            this.lod = lod;
        }

        void OnMeshDataReceived(MeshData meshData) {
            mesh = meshData.CreateMesh();
            hasMesh = true;

            updateCallback();
        }
        
        public void RequestMesh(MapData mapData) {
            hasRequestedMesh = true;
            _mapGenerator.RequestMeshData(mapData, lod, OnMeshDataReceived);
        }
    }

    [System.Serializable]
    public struct LODInfo {
        [Range(0, MeshGenerator.numSupportedLODs-1)]
        public int lod;
        public float visibleDstThreshold;
        
        public float sqrVisibleDstThreshold {
            get {
                return visibleDstThreshold * visibleDstThreshold;
            }
        }
    }
    
    #endregion
}