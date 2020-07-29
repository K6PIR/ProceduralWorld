using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForestGenerator : MonoBehaviour {
    public static List<Vector2> AddTrees(Forest mainForest, ForestSettings settings, MeshSettings meshSettings, Vector2 coord,
        HeightMap heightMap, Transform parent) {
        int width = heightMap.values.GetLength(0);
        int height = heightMap.values.GetLength(1);
        List<Vector2> treeKeys = new List<Vector2>();

        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                for (int i = 0; i < settings.forestElements.Length; i++) {
                    Vector3 newPosition = new Vector3(
                        (coord.x + x - height / 2) * meshSettings.meshScale,
                        heightMap.values[x, y] - settings.forestElements[i].yOffset,
                        (coord.y - y + (height / 2)) * meshSettings.meshScale);

                    if (heightMap.values[x, y] > settings.forestElements[i].minHeight &&
                        heightMap.values[x, y] < settings.forestElements[i].maxHeight &&
                        CanPlantTree(mainForest, new Vector2(newPosition.x, newPosition.z), settings.forestElements[i].separation) &&
                        Random.value < settings.forestElements[i].ratio) {
                        
                        ForestElement newForestElement = new ForestElement();
                        newForestElement.name = settings.forestElements[i].name;
                        newForestElement.prefab = Instantiate(settings.forestElements[i].prefab, parent);

                        Vector3 treeRotation = newForestElement.prefab.transform.eulerAngles;
                        treeRotation.y = Random.Range(-180, 180);
                        newForestElement.prefab.transform.eulerAngles = treeRotation;
                        
                        newForestElement.prefab.transform.localScale = Vector3.one * Random.Range(0.5f, 1.5f);
                        
//                        Vector3 newPosition = new Vector3(
//                            (coord.x + x - height / 2) * meshSettings.meshScale,
//                            heightMap.values[x, y] - settings.forestElements[i].yOffset,
//                            (coord.y - y + (height / 2)) * meshSettings.meshScale);
//                        
                        newForestElement.prefab.transform.position = newPosition + new Vector3((Random.value - 0.5f), 0, (Random.value - 0.5f));
                        Vector2 forestElementKey = new Vector2(newPosition.x, newPosition.z);
                        mainForest.trees.Add(forestElementKey, newForestElement);
                        treeKeys.Add(forestElementKey);
                    }
                }
            }
        }
        return treeKeys;
    }

    private static bool CanPlantTree(Forest forest, Vector2 position,  int separation) {

        for (int y = (int)position.y - separation; y < position.y + separation; y++) {
            for (int x = (int) position.x - separation; x < position.x + separation; x++) {
                if (forest.trees.ContainsKey(new Vector2(x, y))) return false;
            }
        }
        return true;
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