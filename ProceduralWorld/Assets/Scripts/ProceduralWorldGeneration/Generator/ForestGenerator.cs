using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForestGenerator : MonoBehaviour {
    
    public static List<Vector2> AddTrees(Forest mainForest, ForestSettings settings, MeshSettings meshSettings, Vector2 coord,
        HeightMap heightMap, Transform parent) {
        int width = heightMap.values.GetLength(0);
        int height = heightMap.values.GetLength(1);
        List<Vector2> treeKeys = new List<Vector2>();

        float[][,] values = new float[settings.forestElements.Length][,];
        for (int i = 0; i < settings.forestElements.Length; i++) {
            values[i] = Noise.GenerateNoiseMap(width, height, settings.forestElements[i].noiseSettings, coord);
        }

        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                for (int i = 0; i < settings.forestElements.Length; i++) {
                    Vector3 newPosition = new Vector3((coord.x + x - height / 2) * meshSettings.meshScale,
                        heightMap.values[x, y] - settings.forestElements[i].yOffset,
                        (coord.y - y + (height / 2)) * meshSettings.meshScale);

                    float perlin = values[i][x, y];

                    if (heightMap.values[x, y] > settings.forestElements[i].minHeight &&
                        heightMap.values[x, y] < settings.forestElements[i].maxHeight &&
                        CanPlantTree(mainForest, new Vector2(newPosition.x, newPosition.z), settings.forestElements[i].separation, meshSettings.meshScale) &&
                        perlin < settings.forestElements[i].perlinRatio && Random.value < settings.forestElements[i].densityRatio) {

                        Vector3 treeRotation = Vector3.zero;
                        treeRotation.y = Random.Range(-180, 180);

                        Vector3 scale = Vector3.one * Random.Range(0.5f, 1.5f);
                        Vector2 forestElementKey = new Vector2(newPosition.x, newPosition.z);

                        ForestElement newForestElement = new ForestElement();
                        newForestElement.element = Instantiate(settings.forestElements[i].prefab, parent);
                        newForestElement.Initialize(newPosition, treeRotation, scale);

                        mainForest.trees.Add(forestElementKey, newForestElement);
                        treeKeys.Add(forestElementKey);
                    }
                }
            }
        }

        return treeKeys;
    }

    private static bool CanPlantTree(Forest forest, Vector2 position, int separation, float scale) {
        for (int y = -separation; y < separation; y++) {
            for (int x = -separation; x < separation; x++) {
                if (forest.trees.ContainsKey(new Vector2(position.x + x * scale, position.y + y * scale))) {
                    return false;
                }
            }
        }

        return true;
    }
}

public class Forest {
    public Dictionary<Vector2, ForestElement> trees = new Dictionary<Vector2, ForestElement>();
}

public class ForestElement {
    public GameObject element;

    public ForestElement() {
    }

    public void Initialize(Vector3 position, Vector3 rotation, Vector3 scale) {
        element.transform.eulerAngles = rotation;
        element.transform.localScale = scale;
        element.transform.position = position;
    }
}