using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdatableData : ScriptableObject {
    public event System.Action OnValuesUpdated;

    public bool autoUpdate;

    protected virtual void OnValidate() {
        if (autoUpdate) {
            NotifyOfUpdatedValue();
        }
    }
    
    public void NotifyOfUpdatedValue() {
        if (OnValuesUpdated != null) OnValuesUpdated();
    }
}
