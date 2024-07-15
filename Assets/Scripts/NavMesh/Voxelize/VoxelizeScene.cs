using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// This class voxellizes a scene and creates a grid of vertical spans.
/// </summary>
public class VoxelizeScene : MonoBehaviour
{
    public float XZCellSize;
    public float YCellSize;

    Heightfield sceneField;

    [System.NonSerialized]
    public bool sceneVoxed = false;

    public DebugHeightSpanDrawMode debugMode;
    public float maxWalkableSlope = 45f;

    private Triangle[] GetWalkableTriangles(Mesh combinedSceneMesh)
    {
        List<Triangle> walkableTriangles = new List<Triangle>();
        var trianglesToCheck = combinedSceneMesh.triangles;
        var vertices = combinedSceneMesh.vertices;

        for (int i = 0; i < trianglesToCheck.Length; i+=3)
        {
            Triangle tri = new Triangle(vertices[trianglesToCheck[i]], vertices[trianglesToCheck[i + 1]], vertices[trianglesToCheck[i + 2]]);
            //if(Vector3.Angle(Vector3.up, tri.Normal) <= maxWalkableSlope && Vector3.Angle(Vector3.up, tri.Normal) >= -maxWalkableSlope)
            {
                walkableTriangles.Add(tri);
            }
        }

        return walkableTriangles.ToArray();
    }

    public void VoxelizeSceneByCombiningMeshes()
    {
        var allSceneMeshes = FindObjectsOfType<MeshFilter>().Where(x => x.gameObject.isStatic == true);
        
        Mesh sceneMesh = new Mesh();
        CombineInstance[] meshesToCombine = new CombineInstance[allSceneMeshes.Count()];
        for(int i = 0; i < allSceneMeshes.Count(); i++)
        {
            meshesToCombine[i].mesh = allSceneMeshes.ElementAt(i).sharedMesh;
            meshesToCombine[i].transform = allSceneMeshes.ElementAt(i).transform.localToWorldMatrix;

        }
        sceneMesh.CombineMeshes(meshesToCombine);

        sceneField = new Heightfield(XZCellSize, YCellSize);

        sceneField.CreateHeightFieldGrid(sceneMesh.bounds);
        sceneField.CheckHeightfieldAgainstTriangles(GetWalkableTriangles(sceneMesh), sceneMesh);
        sceneField.ConvertHeightfieldGridToSpans(sceneMesh);
        sceneVoxed = true;
    }

    public Heightfield GetHeightfield()
    {
        return sceneField;
    }

    private void OnDrawGizmosSelected()
    {
        if (!sceneVoxed)
            return;

        if (sceneField == null)
            return;
            
        var grid = sceneField.GetVerticalSpans();

        if (grid == null)
            return;
            
        for(int xIndex = 0; xIndex < sceneField.gridRowsX - 1; xIndex++)
        {
            for(int zIndex = 0; zIndex < sceneField.gridRowsZ - 1; zIndex++)
            {

                foreach(var item in grid[xIndex, zIndex])
                {
                    bool drawCube = false;
                    switch (debugMode)
                    {
                        case DebugHeightSpanDrawMode.Both:
                            { 
                                if (item.type == VoxelType.Closed)
                                {
                                    Gizmos.color = Color.red;
                                }
                                else
                                {
                                    Gizmos.color = Color.green;
                                }
                                drawCube = true;
                                break;
                            }
                        case DebugHeightSpanDrawMode.ClosedSpans:
                            {
                                Gizmos.color = Color.red;
                                if (item.type == VoxelType.Closed)
                                {
                                    drawCube = true;
                                }
                                break;
                            }
                        case DebugHeightSpanDrawMode.OpenSpans:
                            {
                                Gizmos.color = Color.green;
                                if (item.type == VoxelType.Open)
                                {
                                    drawCube = true;
                                }
                                break;
                            }
                    }
                    if (drawCube)
                    {
                        
                        //draw a cube representing the span
                        var spanBounds = item.SpanBounds;
                        Gizmos.DrawCube(spanBounds.Center, spanBounds.Max - spanBounds.Min);

                        //draw an outline for the span
                        Gizmos.color = Color.black;
                        Gizmos.DrawWireCube(spanBounds.Center, spanBounds.Max - spanBounds.Min);
                    }
                }
            }
        }
    }

    public enum DebugHeightSpanDrawMode
    {
        ClosedSpans,
        OpenSpans,
        Both
    };
}

