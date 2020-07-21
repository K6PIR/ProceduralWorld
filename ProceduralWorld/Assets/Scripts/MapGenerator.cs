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
        ColorMap,
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
    
    [Range(0, 6)] public int editorPreviewLOD;
    

    public bool autoUpdate;

    public TerrainType[] regions;

    public static MapGenerator instance;
    
    #endregion

    #region Private Variables

    private float[,] _falloffMap;

    Queue<MapThreadInfo<MapData>> _mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> _meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    #endregion

    #region Public Methods

    public static int MapChunkSize {
        get {
            if (instance == null) {
                instance = FindObjectOfType<MapGenerator>();
            }
            if (instance.terrainData.useFlatShading)
                return 95;
            else
                return 239;
        }
    }

    public void DrawMapInEditor() {
        MapDisplay display = FindObjectOfType<MapDisplay>();

        MapData mapData = GenerateMapData(Vector2.zero);

        switch (drawMode) {
            case DrawMode.NoiseMap:
                display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
                break;
            case DrawMode.ColorMap:
                display.DrawTexture(TextureGenerator.TextureFromColorMap(mapData.colorMap, MapChunkSize, MapChunkSize));
                break;
            case DrawMode.Mesh:
                display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrainData.meshHeightMultiplier,
                        terrainData.meshHeightCurve,
                        editorPreviewLOD, terrainData.useFlatShading),
                    TextureGenerator.TextureFromColorMap(mapData.colorMap, MapChunkSize, MapChunkSize));
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
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrainData.meshHeightMultiplier, terrainData.meshHeightCurve,
            lod, terrainData.useFlatShading);

        lock (_meshDataThreadInfoQueue) {
            _meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    #endregion

    #region Private Methods

    private MapData GenerateMapData(Vector2 centre) {
        float[,] noiseMap = Noise.GenerateNoiseMap(MapChunkSize + 2, MapChunkSize + 2, noiseData.seed, noiseData.noiseScale, noiseData.octaves,
            noiseData.persistence,
            noiseData.lacunarity, centre + noiseData.offset, noiseData.normalizeMode);

        Color[] colorMap = new Color[MapChunkSize * MapChunkSize];

        for (int y = 0; y < MapChunkSize; y++) {
            for (int x = 0; x < MapChunkSize; x++) {
                if (terrainData.useFalloff) {
                    noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - _falloffMap[x, y]);
                }

                float currentHeight = noiseMap[x, y];
                for (int i = 0; i < regions.Length; i++) {
                    if (currentHeight >= regions[i].height) {
                        colorMap[y * MapChunkSize + x] = regions[i].color;
                    } else {
                        break;
                    }
                }
            }
        }

        return new MapData(noiseMap, colorMap);
    }

    #endregion

    #region MonoBehaviour CallBack Methods

    private void Awake() {
        _falloffMap = FalloffGenerator.GenerateFalloffMap(MapChunkSize);
    }

    void OnValuesUpdated() {
        if (!Application.isPlaying) {
            DrawMapInEditor();
        }
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
        
        _falloffMap = FalloffGenerator.GenerateFalloffMap(MapChunkSize);
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

[System.Serializable]
public struct TerrainType {
    public string name;
    public float height;
    public Color color;
}

public struct MapData {
    public readonly float[,] heightMap;
    public readonly Color[] colorMap;

    public MapData(float[,] heightMap, Color[] colorMap) {
        this.heightMap = heightMap;
        this.colorMap = colorMap;
    }
}