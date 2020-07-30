using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class ForestSettings : UpdatableData {
    public ForestElementSettings[] forestElements;
    
#if UNITY_EDITOR
    protected override void OnValidate() {
        base.OnValidate();
        for (int i = 0; i < forestElements.Length; i++) {
            forestElements[i].OnValidate();
        }
    }

#endif
}

[System.Serializable]
public class ForestElementSettings {
    public string name;
    public float minHeight;
    public float maxHeight;
    public int separation;
    public float yOffset;

    [Range(0, 1)]
    public float perlinRatio;

    [Range(0, 1)]
    public float densityRatio;
    
    public GameObject prefab;

    public NoiseSettings noiseSettings;
    
    
    public void OnValidate() {
        if (minHeight >= maxHeight) maxHeight = minHeight + 0.01f;
    }
}