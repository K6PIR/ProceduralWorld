using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapPreview : MonoBehaviour
{
    #region Enum

    public enum DrawMode {
        NoiseMap,
        Mesh,
        Falloff
    }

    #endregion
    
    #region Public Variables
    
    public Renderer textureRenderer;

    public MeshFilter meshfilter;
    public MeshRenderer meshRenderer;

    public DrawMode drawMode;

    public MeshSettings meshSettings;
    public HeightMapSettings heightMapSettings;
    public TextureData textureData;

    public Material terrainMaterial;
    
    [Range(0, MeshSettings.numSupportedLODs-1)] public int editorPreviewLOD;


    public bool autoUpdate;

    #endregion

    #region Public Methods
    
    public void DrawMapInEditor() {
        textureData.ApplyToMaterial(terrainMaterial);
        textureData.UpdateMeshHeights(terrainMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);
        
        HeightMap heightMap = HeightMapGenerator.Generate(meshSettings.numVertsPerLine, meshSettings.numVertsPerLine, heightMapSettings, Vector2.zero);

        switch (drawMode) {
            case DrawMode.NoiseMap:
                DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap));
                break;
            case DrawMode.Mesh:
                DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, editorPreviewLOD));
                break;
            case DrawMode.Falloff:
                DrawTexture(TextureGenerator.TextureFromHeightMap(
                    new HeightMap(FalloffGenerator.GenerateFalloffMap(meshSettings.numVertsPerLine), 0, 1)));
                break;
            default:
                break;
        }
    }
    
    public void DrawTexture(Texture2D texture) {

        textureRenderer.sharedMaterial.mainTexture = texture;
        textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height) / 10f;
        
        textureRenderer.gameObject.SetActive(true);
        meshfilter.gameObject.SetActive(false);
    }

    public void DrawMesh(MeshData meshData) {
        meshfilter.sharedMesh = meshData.CreateMesh();

        textureRenderer.gameObject.SetActive(false);
        meshfilter.gameObject.SetActive(true);
    }
    
    #endregion

    #region MonoBehaviour CallBack Methods

    void Start() {
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
        if (meshSettings != null) {
            meshSettings.OnValuesUpdated -= OnValuesUpdated;
            meshSettings.OnValuesUpdated += OnValuesUpdated;
        }

        if (heightMapSettings != null) {
            heightMapSettings.OnValuesUpdated -= OnValuesUpdated;
            heightMapSettings.OnValuesUpdated += OnValuesUpdated;
        }
        
        if (textureData != null) {
            textureData.OnValuesUpdated -= OnTextureValuesUpdated;
            textureData.OnValuesUpdated += OnTextureValuesUpdated;
        }
    }

    #endregion
}
