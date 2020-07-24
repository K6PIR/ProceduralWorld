using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class waterFollowViewer : MonoBehaviour {

    public Transform viewer;

    void Update() {
        transform.position = new Vector3(viewer.position.x, transform.position.y, viewer.position.z);
    }
}
