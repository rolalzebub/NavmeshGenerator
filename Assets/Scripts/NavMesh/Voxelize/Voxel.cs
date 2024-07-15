using UnityEngine;

/// <summary>
/// Types of voxels
/// </summary>
public enum VoxelType
{
    Closed,
    Open
}


public struct Voxel
{
    public VoxelType type;

    public AABB VoxelBounds;

    public bool isWalkable;
    /// <summary>
    /// Vector3.zero if voxel is open
    /// </summary>
    public Vector3 intersectingTriangleNormal;

    public float XZCellSize, YCellSize;

    public Voxel(Vector3[] _vertices, float XZSize, float YSize)
    {
        type = VoxelType.Open;

        Bounds bounds = new Bounds();
        //vertices = _vertices;

        bounds.min = bounds.max = _vertices[0];

        foreach (var item in _vertices)
        {
            bounds.min = Vector3.Min(item, bounds.min);
            bounds.max = Vector3.Max(item, bounds.max);
        }

        VoxelBounds = new AABB(bounds);

        intersectingTriangleNormal = Vector3.zero;

        XZCellSize = XZSize;
        YCellSize = YSize;

        isWalkable = false;
    }
}
