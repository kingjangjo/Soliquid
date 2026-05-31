using UnityEngine;
using System.Collections.Generic;

public class SpatialHash
{
    private float cellSize;
    private Dictionary<int, List<int>> grid = new Dictionary<int, List<int>>();

    public SpatialHash(float cellSize)
    {
        this.cellSize = cellSize;
    }

    int Hash(int x, int y, int z)
    {
        return (x * 73856093) ^ (y * 19349663) ^ (z * 83492791);
    }

    Vector3Int GetCell(Vector3 pos)
    {
        return new Vector3Int(
            Mathf.FloorToInt(pos.x / cellSize),
            Mathf.FloorToInt(pos.y / cellSize),
            Mathf.FloorToInt(pos.z / cellSize)
        );
    }

    public void Rebuild(List<Particle> particles)
    {
        grid.Clear();
        for (int i = 0; i < particles.Count; i++)
        {
            var cell = GetCell(particles[i].position);
            int h = Hash(cell.x, cell.y, cell.z);

            if (!grid.ContainsKey(h))
                grid[h] = new List<int>();

            grid[h].Add(i);
        }
    }

    public void GetNeighborIndices(Vector3 pos, List<int> result)
    {
        result.Clear();
        var cell = GetCell(pos);

        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
                for (int dz = -1; dz <= 1; dz++)
                {
                    int h = Hash(cell.x + dx, cell.y + dy, cell.z + dz);
                    if (grid.TryGetValue(h, out var list))
                        result.AddRange(list);
                }
    }
}