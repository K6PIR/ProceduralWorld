using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour {
    
    #region Public Variables

    public LODInfo[] detailLevels;
    public static float maxViewDistance;
    
    
    public Transform viewer;

    public static Vector2 viewerPosition;
    

    public Material mapMaterial;

    #endregion

    #region Private Variables

    private const float _scale = 1f;
    
    private const float MoveThresholdForChunkUpdate = 25f;
    private const float SqrMoveThresholdForChunkUpdate = MoveThresholdForChunkUpdate * MoveThresholdForChunkUpdate;

    private Vector2 _viewerPositionOld;
    
    private static MapGenerator _mapGenerator;
    
    private int _chunkSize;
    private int _chunksVisibleInViewDistance;

    private Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    private static List<TerrainChunk> terrainChunkVisibleLastUpdate = new List<TerrainChunk>();
    
    #endregion

    #region MonoBehaviour Callback
    private void Start() {
        maxViewDistance = detailLevels[detailLevels.Length - 1].visibleDstThreshold;
        
        _mapGenerator = FindObjectOfType<MapGenerator>();
        _chunkSize = MapGenerator.MapChunkSize - 1;
        _chunksVisibleInViewDistance = Mathf.RoundToInt(maxViewDistance / _chunkSize);
        
        UpdateVisibleChunks();
    }

    void Update() {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z) / _scale;

        if ((_viewerPositionOld - viewerPosition).sqrMagnitude > SqrMoveThresholdForChunkUpdate) {
            _viewerPositionOld = viewerPosition;
            UpdateVisibleChunks();
        }
    }
    
    #endregion

    #region Private Methods
    
    void UpdateVisibleChunks() {
        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / _chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / _chunkSize);

        for (int i = 0; i < terrainChunkVisibleLastUpdate.Count; i++) {
            terrainChunkVisibleLastUpdate[i].setVisible(false);
        }
        terrainChunkVisibleLastUpdate.Clear();
        
        for (int yOffset = -_chunksVisibleInViewDistance; yOffset <= _chunksVisibleInViewDistance; yOffset++) {
            for (int xOffset = -_chunksVisibleInViewDistance; xOffset <= _chunksVisibleInViewDistance; xOffset++) {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if (terrainChunkDictionary.ContainsKey(viewedChunkCoord)) {
                    terrainChunkDictionary[viewedChunkCoord].Update();
                } else {
                    terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, _chunkSize, detailLevels, transform, mapMaterial));
                }
            }
        }
    }

    #endregion
    
    #region Inner Class
    public class TerrainChunk {
        private GameObject _meshObject;
        private Vector2 _position;
        private Bounds _bounds;
        
        private MeshRenderer _meshRenderer;
        private MeshFilter _meshFilter;
        private MeshCollider _meshCollider;

        private LODInfo[] _detailLevels;
        private LODMesh[] _lodMeshes;

        private MapData _mapData;
        private bool _mapDataReceived;
        private int _previousLODIndex = -1;
        
        public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material material) {
            this._detailLevels = detailLevels;
            
            _position = coord * size;
            _bounds = new Bounds(_position, Vector3.one * size);
            Vector3 positionV3 = new Vector3(_position.x, 0, _position.y);

            _meshObject = new GameObject("TerrainChunk");
            _meshObject.transform.position = positionV3 * _scale;
            _meshObject.transform.parent = parent;
            _meshObject.transform.localScale = Vector3.one * _scale;

            _meshRenderer = _meshObject.AddComponent<MeshRenderer>();
            _meshRenderer.material = material;
            
            _meshFilter = _meshObject.AddComponent<MeshFilter>();

            _meshCollider = _meshObject.AddComponent<MeshCollider>();
            
            setVisible(false);
            
            _lodMeshes = new LODMesh[detailLevels.Length];
            for (int i = 0; i < detailLevels.Length; i++) {
                _lodMeshes[i] = new LODMesh(_detailLevels[i].lod, Update);
            }
            
            _mapGenerator.RequestMapData(_position, OnMapDataReceived);
        }

        void OnMapDataReceived(MapData mapData) {
            _mapData = mapData;
            _mapDataReceived = true;

            Texture2D texture = TextureGenerator.TextureFromColorMap(mapData.colorMap, MapGenerator.MapChunkSize,
                MapGenerator.MapChunkSize);

            _meshRenderer.material.mainTexture = texture;
            
            Update();
        }

        public void Update() {
            if (!_mapDataReceived) return;
            
            float distanceFromNearestEdge = Mathf.Sqrt(_bounds.SqrDistance(viewerPosition));
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
                        _meshCollider.sharedMesh = lodMesh.mesh;
                    } else if ( !lodMesh.hasRequestedMesh) {
                        lodMesh.RequestMesh(_mapData);
                    }
                }

                terrainChunkVisibleLastUpdate.Add(this);
            }
            
            setVisible(visible);
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

        private System.Action _updateCallback;
        
        public LODMesh(int lod, System.Action updateCallback) {
            this.lod = lod;
            this._updateCallback = updateCallback;
        }

        void OnMeshDataReceived(MeshData meshData) {
            mesh = meshData.CreateMesh();
            hasMesh = true;

            _updateCallback();
        }
        
        public void RequestMesh(MapData mapData) {
            hasRequestedMesh = true;
            _mapGenerator.RequestMeshData(mapData, lod, OnMeshDataReceived);
        }
    }

    [System.Serializable]
    public struct LODInfo {
        public int lod;
        public float visibleDstThreshold;
    }
    
    #endregion
}