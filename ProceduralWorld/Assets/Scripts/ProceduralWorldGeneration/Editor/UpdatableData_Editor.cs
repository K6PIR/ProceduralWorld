using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(UpdatableData), true)]
public class UpdatableData_Editor : Editor
{
  public override void OnInspectorGUI() {
    base.OnInspectorGUI();

    UpdatableData data = (UpdatableData) target;

    if (GUILayout.Button("Update")) {
        data.NotifyOfUpdatedValue();
        EditorUtility.SetDirty(target);
        GUIUtility.ExitGUI();
    }
  }
}
