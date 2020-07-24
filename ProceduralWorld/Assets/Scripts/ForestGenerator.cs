using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForestGenerator : MonoBehaviour {
    public static Forest Generate(ForestSettings settings, MeshSettings meshSettings, Vector2 coord,
        HeightMap heightMap, Transform parent) {
        Forest forest = new Forest();

        int width = heightMap.values.GetLength(0);
        int height = heightMap.values.GetLength(1);

        for (int y = 0; y < height; y += 3) {
            for (int x = 0; x < width; x += 3) {
                if (heightMap.values[x, y] > 45 && heightMap.values[x, y] < 55) {
                    ForestElement newForestElement = new ForestElement();
                    newForestElement.name = settings.forestElements[0].name;
                    newForestElement.prefab = Instantiate(settings.forestElements[0].prefab, parent);
                    newForestElement.prefab.transform.position = new Vector3((coord.x + x - height / 2) * meshSettings.meshScale,
                        heightMap.values[x, y],
                        (coord.y - y + (height / 2)) * meshSettings.meshScale);
                    forest.trees.Add(new Vector2(x, y), newForestElement);
                }
            }
        }

        return forest;
    }

    // Start is called before the first frame update
    void Start() {
    }

    // Update is called once per frame
    void Update() {
    }
}

public class Forest {
    public Dictionary<Vector2, ForestElement> trees = new Dictionary<Vector2, ForestElement>();
}