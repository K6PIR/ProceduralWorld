using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapDisplay : MonoBehaviour
{
    public Renderer textureRenderer;

    public MeshFilter meshfilter;
    public MeshRenderer MeshRenderer;
    
    public void DrawTexture(Texture2D texture) {

        textureRenderer.sharedMaterial.mainTexture = texture;
        textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height);
    }

    public void DrawMesh(MeshData meshData) {
        meshfilter.sharedMesh = meshData.CreateMesh();
        meshfilter.transform.localScale = Vector3.one * FindObjectOfType<MapGenerator>().terrainData.uniformScale;
    }
}
