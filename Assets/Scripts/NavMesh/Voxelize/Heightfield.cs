using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;


public class Heightfield
{
    #region Debugging Data

    public Bounds sceneBounds;

    #endregion

    #region Raw pre-heightfield grid data

    public float XZCellSize;
    public float YCellSize;

    public int gridRowsX, gridRowsZ;
    public int gridColumns;

    #endregion

    Vector3[,,] verts;
    

    /// <summary>
    /// 2D Array of list of vertical spans
    /// </summary>
    List<VerticalSpan>[,] HeightFieldSpans;

    Voxel[,,] voxelGrid;

    SpanGraph heightSpanGraph;

    public Heightfield(float _XZCellSize, float _YCellSize)
    {
        XZCellSize = _XZCellSize;
        YCellSize = _YCellSize;

        HeightFieldSpans = new List<VerticalSpan>[0, 0];

    }

    #region Heightfield generation Functions

    public void ConvertHeightfieldGridToSpans(Mesh sceneMesh)
    {
        HeightFieldSpans = new List<VerticalSpan>[gridRowsX - 1, gridRowsZ - 1];
        
        for (int xIndex = 0; xIndex < gridRowsX - 1; xIndex++)
        {
            for(int zIndex = 0; zIndex < gridRowsZ - 1; zIndex++)
            {
                HeightFieldSpans[xIndex, zIndex] = new List<VerticalSpan>();
                
                var currentSpanColumn = HeightFieldSpans[xIndex, zIndex];

                for (int yIndex = 0; yIndex < gridColumns - 1; yIndex++)
                {
                    if (currentSpanColumn.Count < 1)
                    {
                        //create new span to start a new column
                        var newSpan = new VerticalSpan(voxelGrid[xIndex, yIndex, zIndex]);
                        currentSpanColumn.Add(newSpan);
                    }
                    else
                    {
                        //if we're continuing the same span as before
                        if (voxelGrid[xIndex, yIndex, zIndex].type == currentSpanColumn[currentSpanColumn.Count - 1].type)
                        {
                            currentSpanColumn[currentSpanColumn.Count - 1].AddVoxelToSpan(voxelGrid[xIndex, yIndex, zIndex]);
                        }
                        //else start a new span
                        else
                        {
                            var newSpan = new VerticalSpan(voxelGrid[xIndex, yIndex, zIndex]);
                            currentSpanColumn.Add(newSpan);
                        }
                    }

                    HeightFieldSpans[xIndex, zIndex] = currentSpanColumn;
                }
            }
        }
    }

    public void CheckHeightfieldAgainstTriangles(Triangle[] walkableTriangles, Mesh sceneMesh)
    {
        Stopwatch voxellizeTimer = Stopwatch.StartNew();

        //init voxel grid
        voxelGrid = new Voxel[gridRowsX - 1, gridColumns - 1, gridRowsZ - 1];

        //use only walkable triangles to voxelize as a lil optimization
        NativeArray<Triangle> sceneTriangles = new NativeArray<Triangle>(walkableTriangles, Allocator.TempJob);

        NativeArray<Triangle> walkableTris = new NativeArray<Triangle>(walkableTriangles, Allocator.TempJob);

        //create voxels collection
        NativeArray<Voxel> voxelGridFlatPacked = new NativeArray<Voxel>((gridRowsX - 1) * (gridRowsZ - 1) * (gridColumns - 1), Allocator.TempJob);

        for (int xIndex = 0; xIndex < gridRowsX - 1; xIndex++)
        {
            for (int yIndex = 0; yIndex < gridColumns - 1; yIndex++)
            {
                for (int zIndex = 0; zIndex < gridRowsZ - 1; zIndex++)
                {
                    Vector3[] voxelVerts = new Vector3[8]
                    {
                        verts[xIndex, yIndex, zIndex], verts[xIndex, yIndex + 1, zIndex],
                        verts[xIndex+1, yIndex + 1, zIndex], verts[xIndex+1, yIndex, zIndex],
                        verts[xIndex, yIndex, zIndex + 1], verts[xIndex, yIndex+1, zIndex + 1],
                        verts[xIndex+1, yIndex+1, zIndex+1], verts[xIndex+1, yIndex+1, zIndex + 1]
                    };

                    //flat pack voxels
                    //formula for 3d index to 1d index conversion = x + (y * gridRows) + (z * gridRows * gridColumns)
                    int currentIndex = xIndex + (yIndex * (gridRowsX - 1)) + (zIndex * (gridRowsX - 1) * (gridColumns - 1));
                    Voxel voxel = new Voxel(voxelVerts, XZCellSize, YCellSize);
                    voxelGridFlatPacked[currentIndex] = voxel;
                }
            }
        }

        VoxelFieldGenerationJob vJob = new VoxelFieldGenerationJob()
        {
            voxelField = voxelGridFlatPacked,
            triangles = sceneTriangles,
            walkableTriangles = walkableTris
        };

        //randomly selected a number for num of threads
        //chose 16 because i know my cpu has those many threads
        //this threadCount determination should be dynamically done
        int threadCount = 16;
        int innerLoopBatchCount = voxelGridFlatPacked.Count() / threadCount;
        
        //clamp innerLoopBatchCount to a lower limit of 1, and no upper limit
        innerLoopBatchCount = innerLoopBatchCount < 1 ? 1 : innerLoopBatchCount;

        JobHandle vJobHandle = vJob.Schedule(voxelGridFlatPacked.Count(), voxelGridFlatPacked.Count() / threadCount);

        vJobHandle.Complete();

        //unpack voxels into grid
        for (int xIndex = 0; xIndex < gridRowsX - 1; xIndex++)
        {
            for (int yIndex = 0; yIndex < gridColumns - 1; yIndex++)
            {
                for (int zIndex = 0; zIndex < gridRowsZ - 1; zIndex++)
                {
                    int voxelIndex = xIndex + (yIndex * (gridRowsX - 1)) + (zIndex * (gridRowsX - 1) * (gridColumns - 1));

                    voxelGrid[xIndex, yIndex, zIndex] = vJob.voxelField[voxelIndex];
                }
            }
        }


        voxelGridFlatPacked.Dispose();
        sceneTriangles.Dispose();
        walkableTris.Dispose();

        voxellizeTimer.Stop();

        UnityEngine.Debug.Log("Voxellization took " + voxellizeTimer.ElapsedMilliseconds + "ms");

        #region old method single threaded and main thread blocking way of creating voxel field
        //this code has been left here for documenting reasons
        //new method is 61% faster
        //old method: 8.6 seconds
        //new method: 3.3 seconds

        //List<Triangle> trianglesList = new List<Triangle>(walkableTriangles);

        //for (int xIndex = 0; xIndex < gridRows - 1; xIndex++)
        //{
        //    for (int yIndex = 0; yIndex < gridColumns - 1; yIndex++)
        //    {
        //        for (int zIndex = 0; zIndex < gridRows - 1; zIndex++)
        //        {
        //            Vector3[] voxelVerts = new Vector3[8]
        //            {
        //                verts[xIndex, yIndex, zIndex], verts[xIndex, yIndex + 1, zIndex],
        //                verts[xIndex+1, yIndex + 1, zIndex], verts[xIndex+1, yIndex, zIndex],
        //                verts[xIndex, yIndex, zIndex + 1], verts[xIndex, yIndex+1, zIndex + 1],
        //                verts[xIndex+1, yIndex+1, zIndex+1], verts[xIndex+1, yIndex+1, zIndex + 1]
        //            };

        //            HeightfieldVoxel voxel = new HeightfieldVoxel(voxelVerts, XZCellSize, YCellSize);

        //            for (int i = 0; i < sceneMesh.triangles.Length; i += 3)
        //            {
        //                Triangle tri = new Triangle(sceneMesh.vertices[sceneMesh.triangles[i]], sceneMesh.vertices[sceneMesh.triangles[i + 1]], sceneMesh.vertices[sceneMesh.triangles[i + 2]]);

        //                bool result = Intersections.Intersects(tri, voxel.VoxelBounds);

        //                voxel.isWalkable = trianglesList.Contains(tri);

        //                if (result)
        //                {
        //                    voxel.type = HeightFieldVoxelType.Closed;
        //                    voxel.intersectingTriangleNormal = tri.Normal;

        //                    voxelGrid[xIndex, yIndex, zIndex] = voxel;
        //                    break;
        //                }
        //                else
        //                {
        //                    voxel.type = HeightFieldVoxelType.Open;
        //                }

        //                voxelGrid[xIndex, yIndex, zIndex] = voxel;
        //            }

        //        }
        //    }
        //}
        #endregion
    }

    public void CreateHeightFieldGrid(Bounds _sceneBounds)
    {
        sceneBounds = _sceneBounds;

        Bounds voxelBound = new Bounds();
        voxelBound.size = new Vector3(XZCellSize, YCellSize, XZCellSize);

        int XCellsCount = Mathf.CeilToInt(_sceneBounds.size.x / XZCellSize);
        int ZCellsCount = Mathf.CeilToInt(_sceneBounds.size.z / XZCellSize);
        int YCellsCount = Mathf.CeilToInt(_sceneBounds.size.y / YCellSize);

        gridRowsX = XCellsCount + 1;
        gridRowsZ = ZCellsCount + 1;
        gridColumns = YCellsCount + 1;


        verts = new Vector3[gridRowsX, gridColumns, gridRowsZ];

        Vector3 startPos = new Vector3();
        bool firstVert = false;

        //create vertices to represent the heightfield grid
        for (int xIndex = 0; xIndex < gridRowsX; xIndex++)
        {
            for (int yIndex = 0; yIndex < gridColumns; yIndex++)
            {
                for (int zIndex = 0; zIndex < gridRowsZ; zIndex++)
                {
                    if (!firstVert)
                    {
                        startPos = _sceneBounds.min - new Vector3(XZCellSize / 2, YCellSize / 2, XZCellSize / 2);
                        verts[0, 0, 0] = startPos;
                        firstVert = true;

                        UnityEngine.Debug.Log(_sceneBounds.min);
                    }
                    else
                    {
                        //create a grid for voxelizing mesh
                        Vector3 newVert = new Vector3()
                        {
                            x = startPos.x + (xIndex * XZCellSize),
                            y = startPos.y + (yIndex * YCellSize),
                            z = startPos.z + (zIndex * XZCellSize)
                        };
                        verts[xIndex,yIndex,zIndex] = newVert;
                    }
                }

            }

        }
    }

    #endregion
    
    #region Getters for debugging
    public Voxel[,,] GetVoxelGrid()
    {
        return voxelGrid;
    }

    public List<VerticalSpan>[,] GetVerticalSpans()
    {
        return HeightFieldSpans;
    }

    public Vector3[,,] GetVertGrid()
    {
        return verts;
    }
    #endregion
}

/// <summary>
/// This represents one face on a span or voxel.
/// </summary>
public struct HQuad
{
    public Vector3 bottomLeft, topLeft, topRight, bottomRight;
}

struct VoxelFieldGenerationJob : IJobParallelFor
{
    public NativeArray<Voxel> voxelField;

    public NativeArray<Triangle> triangles;

    public NativeArray<Triangle> walkableTriangles;


    //simply check if the bounding box for a triangle intersects with a voxel in the grid
    //doing this way is faster than only checking triangles by ~8-9 s
    //doing this way is faster than checking box intersection, then checking triangle intersection by ~200-300 ms
    //the time saved justifies the marginally reduced accuracy of voxellization
    //results are based on average of 10 test runs of both methods
    public void Execute(int index)
    {
        if (voxelField[index].type == VoxelType.Closed)
        {
            return;
        }

        foreach(Triangle triangle in triangles)
        {
            if(triangle.Bounds.Intersects(voxelField[index].VoxelBounds.ToBounds()))
            {
                //voxelField[index] = VoxelType.Closed --> can't do this because CS1612

                Voxel v = voxelField[index];
                v.type = VoxelType.Closed;
                v.intersectingTriangleNormal = triangle.Normal;

                if (walkableTriangles.Contains(triangle))
                {
                    v.isWalkable = true;
                }

                voxelField[index] = v;

                return;
            }
        }
    }
}