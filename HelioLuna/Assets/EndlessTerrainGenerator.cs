using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrainGenerator : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Material terrainMaterial;

    [Header("Chunk Settings")]
    public int chunkSize = 64;
    public int viewDistanceInChunks = 3;
    public int resolution = 32;

    [Header("Height Settings")]
    public float heightMultiplier = 12f;
    public float noiseScale = 60f;
    public int seed = 12345;

    private Dictionary<Vector2Int, GameObject> activeChunks = new();
    private Vector2Int currentPlayerChunk;

    private void Start()
    {
        UpdateChunks(true);
    }

    private void Update()
    {
        Vector2Int newChunk = GetPlayerChunk();

        if (newChunk != currentPlayerChunk)
        {
            currentPlayerChunk = newChunk;
            UpdateChunks(false);
        }
    }

    Vector2Int GetPlayerChunk()
    {
        return new Vector2Int(
            Mathf.FloorToInt(player.position.x / chunkSize),
            Mathf.FloorToInt(player.position.z / chunkSize)
        );
    }

    void UpdateChunks(bool force)
    {
        Vector2Int playerChunk = GetPlayerChunk();

        HashSet<Vector2Int> neededChunks = new();

        for (int x = -viewDistanceInChunks; x <= viewDistanceInChunks; x++)
        {
            for (int y = -viewDistanceInChunks; y <= viewDistanceInChunks; y++)
            {
                Vector2Int chunkCoord = playerChunk + new Vector2Int(x, y);
                neededChunks.Add(chunkCoord);

                if (!activeChunks.ContainsKey(chunkCoord))
                {
                    CreateChunk(chunkCoord);
                }
            }
        }

        List<Vector2Int> chunksToRemove = new();

        foreach (var chunk in activeChunks)
        {
            if (!neededChunks.Contains(chunk.Key))
                chunksToRemove.Add(chunk.Key);
        }

        foreach (Vector2Int coord in chunksToRemove)
        {
            Destroy(activeChunks[coord]);
            activeChunks.Remove(coord);
        }
    }

    void CreateChunk(Vector2Int coord)
    {
        GameObject chunk = new GameObject($"Chunk {coord.x}, {coord.y}");
        chunk.transform.parent = transform;
        chunk.transform.position = new Vector3(coord.x * chunkSize, 0, coord.y * chunkSize);

        MeshFilter meshFilter = chunk.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = chunk.AddComponent<MeshRenderer>();
        MeshCollider meshCollider = chunk.AddComponent<MeshCollider>();

        meshRenderer.material = terrainMaterial;

        Mesh mesh = GenerateMesh(coord);
        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;

        activeChunks.Add(coord, chunk);
    }

    Mesh GenerateMesh(Vector2Int coord)
    {
        Mesh mesh = new Mesh();

        int vertsPerLine = resolution + 1;
        Vector3[] vertices = new Vector3[vertsPerLine * vertsPerLine];
        int[] triangles = new int[resolution * resolution * 6];
        Vector2[] uvs = new Vector2[vertices.Length];

        float step = (float)chunkSize / resolution;

        int vertIndex = 0;
        int triIndex = 0;

        for (int z = 0; z <= resolution; z++)
        {
            for (int x = 0; x <= resolution; x++)
            {
                float worldX = coord.x * chunkSize + x * step;
                float worldZ = coord.y * chunkSize + z * step;

                float noise = Mathf.PerlinNoise(
                    (worldX + seed) / noiseScale,
                    (worldZ + seed) / noiseScale
                );

                float height = noise * heightMultiplier;

                vertices[vertIndex] = new Vector3(x * step, height, z * step);
                uvs[vertIndex] = new Vector2((float)x / resolution, (float)z / resolution);

                if (x < resolution && z < resolution)
                {
                    triangles[triIndex + 0] = vertIndex;
                    triangles[triIndex + 1] = vertIndex + vertsPerLine;
                    triangles[triIndex + 2] = vertIndex + 1;

                    triangles[triIndex + 3] = vertIndex + 1;
                    triangles[triIndex + 4] = vertIndex + vertsPerLine;
                    triangles[triIndex + 5] = vertIndex + vertsPerLine + 1;

                    triIndex += 6;
                }

                vertIndex++;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }
}