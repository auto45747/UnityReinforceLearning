using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class TrainingGridManager : MonoBehaviour
{
    [Header("Prefab & Layout")]
    [SerializeField] private GameObject platformPrefab;
    [SerializeField] private int totalPlatforms = 30;
    [SerializeField] private int columns = 6;

    [Header("Dynamic Spacing")]
    [Tooltip("Extra empty space between adjacent platforms.")]
    [SerializeField] private float padding = 5f;

    [ContextMenu("Generate Grid")]
    public void GenerateGrid()
    {
        // 1. Clear existing generated platforms
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }

        if (platformPrefab == null)
        {
            Debug.LogError("Assign a platformPrefab first!");
            return;
        }

        // 2. Measure platform physical size from its Collider or Transform scale
        Collider col = platformPrefab.GetComponentInChildren<Collider>();
        float platformSizeX = (col != null) ? col.bounds.size.x : platformPrefab.transform.localScale.x;
        float platformSizeZ = (col != null) ? col.bounds.size.z : platformPrefab.transform.localScale.z;

        // Total distance per step (Platform Dimension + Dynamic Padding)
        float stepX = platformSizeX + padding;
        float stepZ = platformSizeZ + padding;

        // 3. Instantiate and position platforms in a grid
        for (int i = 0; i < totalPlatforms; i++)
        {
            int row = i / columns;
            int colIdx = i % columns;

            Vector3 spawnPos = new Vector3(colIdx * stepX, 0f, row * stepZ);

            #if UNITY_EDITOR
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(platformPrefab, transform);
            instance.transform.position = spawnPos;
            #else
            Instantiate(platformPrefab, spawnPos, Quaternion.identity, transform);
            #endif
        }

        Debug.Log($"Generated {totalPlatforms} platforms with {padding}m dynamic padding.");
    }
}