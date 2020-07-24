using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class ForestSettings : UpdatableData {
    public ForestElement[] forestElements;

//    public AnimationCurve heightCurve;





  
#if UNITY_EDITOR
    protected override void OnValidate() {
        base.OnValidate();
    }
    
#endif
}


[System.Serializable]
public class ForestElement {

    public string name;
    public float yOffset;
    public GameObject prefab;
}
