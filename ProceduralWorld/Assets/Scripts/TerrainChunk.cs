using UnityEngine;

public class TerrainChunk {
    public event System.Action<TerrainChunk, bool> onVisibilityChanged; 
    public Vector2 coord;

    private const float ColliderGenerationDistanceThreshold = 5;

    private GameObject _meshObject;
    private Vector2 _sampleCentre;
    private Bounds _bounds;

    private MeshRenderer _meshRenderer;
    private MeshFilter _meshFilter;
    private MeshCollider _meshCollider;
    
    private Forest _forest;

    private HeightMap _heightMap;
    private bool _heightMapReceived;
    
    private LODInfo[] _detailLevels;
    private LODMesh[] _lodMeshes;
    
    private int _colliderLODIndex;
    private int _previousLODIndex = -1;
    private bool _hasSetCollider;
    private float _maxViewDistance;

    private HeightMapSettings _heightMapSettings;
    private MeshSettings _meshSettings;
    private ForestSettings _forestSettings;
    

    private Transform _viewer;
    
    public Vector2 viewerPosition {
        get { return new Vector2(_viewer.position.x, _viewer.position.z); }
    }


    public TerrainChunk(Vector2 coord, HeightMapSettings heightMapSettings, MeshSettings meshSettings, ForestSettings forestSettings,
        LODInfo[] detailLevels, int colliderLODIndex, Transform parent, Transform viewer, Material material) {
        this.coord = coord;
        _detailLevels = detailLevels;
        _colliderLODIndex = colliderLODIndex;
        _heightMapSettings = heightMapSettings;
        _meshSettings = meshSettings;
        _forestSettings = forestSettings;
        _viewer = viewer;

        _sampleCentre = coord * meshSettings.meshWorldSize / meshSettings.meshScale;
        Debug.Log("4  " + _sampleCentre);
        Vector2 position = coord * meshSettings.meshWorldSize;
        _bounds = new Bounds(_sampleCentre, Vector3.one * meshSettings.meshWorldSize);
        
        _meshObject = new GameObject("TerrainChunk");
        _meshObject.transform.position = new Vector3(position.x, 0, position.y);
        _meshObject.transform.parent = parent;

        _meshRenderer = _meshObject.AddComponent<MeshRenderer>();
        _meshRenderer.material = material;

        _meshFilter = _meshObject.AddComponent<MeshFilter>();

        _meshCollider = _meshObject.AddComponent<MeshCollider>();

        _maxViewDistance = _detailLevels[detailLevels.Length - 1].visibleDstThreshold; 
        
        setVisible(false);

        _lodMeshes = new LODMesh[detailLevels.Length];
        for (int i = 0; i < detailLevels.Length; i++) {
            _lodMeshes[i] = new LODMesh(_detailLevels[i].lod);
            _lodMeshes[i].updateCallback += Update;
            if (i == colliderLODIndex) {
                _lodMeshes[i].updateCallback += UpdateCollisionMesh;
            }
        }

    }

    public void Load() {
        ThreadedDataRequester.RequestData(() => HeightMapGenerator.Generate(_meshSettings.numVertsPerLine,
            _meshSettings.numVertsPerLine,
            _heightMapSettings, _sampleCentre), OnHeightMapReceived);
    }
    
    void OnHeightMapReceived(object heightMapObject) {
        _heightMap = (HeightMap) heightMapObject;
        _heightMapReceived = true;
        Update();
        _forest = ForestGenerator.Generate(_forestSettings, _meshSettings, _sampleCentre, _heightMap, _meshObject.transform.parent);
    }

    public void Update() {
        if (!_heightMapReceived) return;

        float distanceFromNearestEdge = Mathf.Sqrt(_bounds.SqrDistance(viewerPosition));

        bool wasVisible = IsVisible();
        bool visible = distanceFromNearestEdge <= _maxViewDistance;

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
                } else if (!lodMesh.hasRequestedMesh) {
                    lodMesh.RequestMesh(_heightMap, _meshSettings);
                }
            }
        }

        if (wasVisible != visible) {
            setVisible(visible);
            if (onVisibilityChanged != null) {
                onVisibilityChanged(this, visible);
            }
        }

        setVisible(visible);
    }

    public void UpdateCollisionMesh() {
        if (_hasSetCollider) return;

        float sqrDstFromViewerToEdge = _bounds.SqrDistance(viewerPosition);

        if (sqrDstFromViewerToEdge < _detailLevels[_colliderLODIndex].sqrVisibleDstThreshold) {
            if (!_lodMeshes[_colliderLODIndex].hasRequestedMesh) {
                _lodMeshes[_colliderLODIndex].RequestMesh(_heightMap, _meshSettings);
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

    void OnMeshDataReceived(object meshDataObject) {
        mesh = ((MeshData) meshDataObject).CreateMesh();
        hasMesh = true;

        updateCallback();
    }

    public void RequestMesh(HeightMap heightMap, MeshSettings meshSettings) {
        hasRequestedMesh = true;

        ThreadedDataRequester.RequestData(() => MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, lod),
            OnMeshDataReceived);
    }
}