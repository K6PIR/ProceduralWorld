using System;
using System.Collections;
using UnityEngine;
using System.Threading;
using System.Collections.Generic;
using UnityEngine.PlayerLoop;

public class MapGenerator : MonoBehaviour {
    #region Enum

    public enum DrawMode {
        NoiseMap,
        Mesh,
        Falloff
    }

    #endregion

    #region Inner Struct

    struct MapThreadInfo<T> {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter) {
            this.callback = callback;
            this.parameter = parameter;
        }
    }

    #endregion

    #region Public Variables

    public DrawMode drawMode;

    public TerrainData terrainData;
    public NoiseData noiseData;
    public TextureData textureData;

    public Material terrainMaterial;

    [Range(0, MeshGenerator.numSupportedChunkSizes-1)]
    public int chunkSizeIndex;
    [Range(0, MeshGenerator.numSupportedFlatShadedChunkSizes-1)]
    public int flatShadedChunkSizeIndex;
    
    [Range(0, MeshGenerator.numSupportedLODs-1)] public int editorPreviewLOD;


    public bool autoUpdate;

    #endregion

    #region Private Variables

    private float[,] _falloffMap;

    Queue<MapThreadInfo<MapData>> _mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> _meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    #endregion

    #region Public Methods

    public int MapChunkSize {
        get {
            if (terrainData.useFlatShading)
                return MeshGenerator.supportedFlatShadedChunkSizes[flatShadedChunkSizeIndex] - 1;
            return MeshGenerator.supportedChunkSizes[chunkSizeIndex] - 1;
        }
    }

    public void DrawMapInEditor() {
        textureData.UpdateMeshHeights(terrainMaterial, terrainData.minHeight, terrainData.maxHeight);

        MapDisplay display = FindObjectOfType<MapDisplay>();

        MapData mapData = GenerateMapData(Vector2.zero);

        switch (drawMode) {
            case DrawMode.NoiseMap:
                display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
                break;
            case DrawMode.Mesh:
                display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrainData.meshHeightMultiplier,
                    terrainData.meshHeightCurve,
                    editorPreviewLOD, terrainData.useFlatShading));
                break;
            case DrawMode.Falloff:
                display.DrawTexture(
                    TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(MapChunkSize)));
                break;
            default:
                break;
        }
    }

    #endregion

    #region Threading Methods

    public void RequestMapData(Vector2 centre, Action<MapData> callback) {
        ThreadStart threadStart = delegate { MapDataTread(centre, callback); };

        new Thread(threadStart).Start();
    }

    private void MapDataTread(Vector2 centre, Action<MapData> callback) {
        MapData mapData = GenerateMapData(centre);

        lock (_mapDataThreadInfoQueue) {
            _mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback) {
        ThreadStart threadStart = delegate { MeshDataTread(mapData, lod, callback); };

        new Thread(threadStart).Start();
    }

    private void MeshDataTread(MapData mapData, int lod, Action<MeshData> callback) {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrainData.meshHeightMultiplier,
            terrainData.meshHeightCurve,
            lod, terrainData.useFlatShading);

        lock (_meshDataThreadInfoQueue) {
            _meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    #endregion

    #region Private Methods

    private MapData GenerateMapData(Vector2 centre) {
        float[,] noiseMap = Noise.GenerateNoiseMap(MapChunkSize + 2, MapChunkSize + 2, noiseData.seed,
            noiseData.noiseScale, noiseData.octaves,
            noiseData.persistence,
            noiseData.lacunarity, centre + noiseData.offset, noiseData.normalizeMode);

        if (terrainData.useFalloff) {

            if (_falloffMap == null) _falloffMap = FalloffGenerator.GenerateFalloffMap(MapChunkSize + 2);
            
            for (int y = 0; y < MapChunkSize + 2; y++) {
                for (int x = 0; x < MapChunkSize + 2; x++) {
                    if (terrainData.useFalloff) {
                        noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - _falloffMap[x, y]);
                    }
                }
            }
        }
        
        return new MapData(noiseMap);
    }

    #endregion

    #region MonoBehaviour CallBack Methods

    void Awake() {
        textureData.ApplyToMaterial(terrainMaterial);
        textureData.UpdateMeshHeights(terrainMaterial, terrainData.minHeight, terrainData.maxHeight);
    }
    
    void OnValuesUpdated() {
        if (!Application.isPlaying) {
            DrawMapInEditor();
        }
    }

    void OnTextureValuesUpdated() {
        textureData.ApplyToMaterial(terrainMaterial);
    }

    private void OnValidate() {
        if (terrainData != null) {
            terrainData.OnValuesUpdated -= OnValuesUpdated;
            terrainData.OnValuesUpdated += OnValuesUpdated;
        }

        if (noiseData != null) {
            noiseData.OnValuesUpdated -= OnValuesUpdated;
            noiseData.OnValuesUpdated += OnValuesUpdated;
        }
        
        if (textureData != null) {
            textureData.OnValuesUpdated -= OnTextureValuesUpdated;
            textureData.OnValuesUpdated += OnTextureValuesUpdated;
        }
    }

    void Update() {
        if (_mapDataThreadInfoQueue.Count > 0) {
            for (int i = 0; i < _mapDataThreadInfoQueue.Count; i++) {
                MapThreadInfo<MapData> threadInfo = _mapDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }

        if (_meshDataThreadInfoQueue.Count > 0) {
            for (int i = 0; i < _meshDataThreadInfoQueue.Count; i++) {
                MapThreadInfo<MeshData> threadInfo = _meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    #endregion
}

public struct MapData {
    public readonly float[,] heightMap;

    public MapData(float[,] heightMap) {
        this.heightMap = heightMap;
    }
}