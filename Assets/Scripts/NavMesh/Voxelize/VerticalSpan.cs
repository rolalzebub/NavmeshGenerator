using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Collection of vertically adjacent voxels where all of them share the same type (i.e. open/closed) and form an un-interrupted sequence.
/// </summary>
public struct VerticalSpan
{
    public VoxelType type;

    List<Voxel> spanVoxels;

    AABB spanBounds;

    public bool canAgentStandHere;

    public AABB SpanBounds
    {
        get
        {
            CalculateSpanBounds();

            return spanBounds;
        }
    }

    public void AddVoxelToSpan(Voxel voxel)
    {
        spanVoxels.Add(voxel);
        CalculateSpanBounds();
    }

    public List<Voxel> GetSpanVoxels()
    {
        return spanVoxels;
    }

    public void CalculateSpanBounds()
    {
        Vector3 min = spanVoxels[0].VoxelBounds.Min;
        Vector3 max = spanVoxels[0].VoxelBounds.Max;

        foreach (var voxel in spanVoxels)
        {
            min = Vector3.Min(min, voxel.VoxelBounds.Min);
            max = Vector3.Max(max, voxel.VoxelBounds.Max);
        }

        spanBounds = new AABB(min, max);
    }

    public VerticalSpan(Voxel startingVoxel)
    {
        spanVoxels = new List<Voxel>() { startingVoxel };

        spanBounds = startingVoxel.VoxelBounds;
        type = startingVoxel.type;

        canAgentStandHere = false;
    }


    /// <summary>
    /// Get vertical size of the span.
    /// </summary>
    public float GetSpanHeight()
    {
        CalculateSpanBounds();

        return SpanBounds.Max.y - SpanBounds.Min.y;
    }

    /// <summary>
    /// Get the bottom face for this span.
    /// </summary>
    public HQuad GetSpanFloor()
    {
        HQuad toReturn = new HQuad();

        toReturn.bottomLeft = SpanBounds.Min;
        toReturn.topLeft = new Vector3(SpanBounds.Min.x, SpanBounds.Min.y, SpanBounds.Max.z);
        toReturn.bottomRight = new Vector3(SpanBounds.Max.x, SpanBounds.Min.y, SpanBounds.Min.z);
        toReturn.topRight = new Vector3(SpanBounds.Max.x, SpanBounds.Min.y, SpanBounds.Max.z);

        return toReturn;
    }

    /// <summary>
    /// Get the top face for this span.
    /// </summary>
    public HQuad GetSpanCeiling()
    {
        HQuad toReturn = new HQuad();

        toReturn.bottomLeft = spanVoxels[spanVoxels.Count - 1].VoxelBounds.Min;
        toReturn.bottomLeft.y = spanVoxels[spanVoxels.Count - 1].VoxelBounds.Max.y;

        toReturn.topLeft = spanVoxels[spanVoxels.Count - 1].VoxelBounds.Max;
        toReturn.topLeft.x = spanVoxels[spanVoxels.Count - 1].VoxelBounds.Min.x;

        toReturn.topRight = spanVoxels[spanVoxels.Count - 1].VoxelBounds.Max;

        toReturn.bottomRight = spanVoxels[spanVoxels.Count - 1].VoxelBounds.Max;
        toReturn.bottomRight.z = spanVoxels[spanVoxels.Count - 1].VoxelBounds.Min.z;

        return toReturn;
    }

    //Get only the world up-axis height of the span
    public float GetSpanCeilingLevel()
    {
        return SpanBounds.Max.y;
    }
}

