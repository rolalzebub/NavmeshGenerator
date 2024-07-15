using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using static UnityEditor.Progress;

/// <summary>
/// This class will take in a heightfield and produce a connection graph of open spans
/// </summary>
public class RegionAnalyzer: MonoBehaviour
{
    SpanGraph graph;

    public float maxStepDistance;
    public float agentHeight;
    public float agentRadius;
    public float stepHeight;
    public float maxWalkableAngle = 45f; 

    [System.NonSerialized]
    public bool graphCreated = false;

    public void CreateSpanGraphFromHeightfield(Heightfield heightfield)
    {
        Stopwatch spanGraphTimer = Stopwatch.StartNew();

        if(graph != null)
        {
            graph = null;
        }

        AgentSettings settings = new AgentSettings();
        settings.maxStepDistance = maxStepDistance;
        settings.agentHeight = agentHeight;
        settings.agentRadius = agentRadius;
        settings.maxStepHeight = stepHeight;
        settings.maxWalkableAngle = maxWalkableAngle;
        
        var heightSpans = heightfield.GetVerticalSpans();
        
        //iterate through all spans and mark spans on which agent can stand
        for(int i = 0; i < heightSpans.GetLength(0); i++)
        {
            for(int j = 0; j < heightSpans.GetLength(1); j++)
            {
                for(int k = 0; k < heightSpans[i,j].Count; k++)
                {
                    //if this is the topmost span in the grid, the player cannot stand on it because level ceiling is reached
                    if(k >= heightSpans[i,j].Count - 1)
                    {
                        continue;
                    }

                    //sanity check to make sure the span above is not closed space
                    //this should never fail because vertical spans should be a continuous column of the same type of voxel until a break is found
                    if (heightSpans[i, j][k+1].type == VoxelType.Closed)
                    {
                        continue;
                    }

                    var span = heightSpans[i, j][k];

                    //ignore open voxels
                    if (span.type == VoxelType.Open)
                    {
                        continue;
                    }

                    //the span above the current one should have enough vertical space for the agent to stand
                    if (heightSpans[i, j][k + 1].GetSpanHeight() < settings.agentHeight)
                    {
                        continue;
                    }

                    //check all span voxels to make sure they are not too steep
                    var spanVoxels = span.GetSpanVoxels();
                    foreach(var v in spanVoxels)
                    {
                        if(!v.isWalkable)
                        {
                            continue;
                        }
                    }

                    //if we made it this far then we've passed all the checks and the span is indeed walkable
                    span.canAgentStandHere = true;
                    
                    heightSpans[i, j][k] = span;
                }
            }
        }


        //create span graph
        for (int i = 0; i < heightSpans.GetLength(0) - 1; i++)
        {
            //add new nodes when the loop cycles over
            if (graph != null)
            {
                graph.allNodes.Add(new List<List<SpanGraph.SpanGraphNode>>());
            }

            for (int j = 0; j < heightSpans.GetLength(1) - 1; j++)
            {
                //add new nodes when the loop cycles over
                if (graph != null)
                {
                    graph.allNodes[i].Add(new List<SpanGraph.SpanGraphNode>());
                }

                for (int k = 0; k < heightSpans[i, j].Count; k++)
                {
                    var currentSpan = heightSpans[i, j][k];

                    SpanGraph.SpanGraphNode currentNode = new SpanGraph.SpanGraphNode(currentSpan);

                    currentNode.isWalkable = currentSpan.canAgentStandHere;


                    //initialize graph
                    if (graph == null)
                    {
                        graph = new SpanGraph(currentNode, settings);
                    }
                    else
                    {
                        graph.allNodes[i][j].Add(currentNode);
                    }
                }
            }
        }

        graph.MakeAllNeighbourConnections();

        graph.FindAllBoundaryNodes();

        spanGraphTimer.Stop();

        UnityEngine.Debug.Log("Span graph creation took " + spanGraphTimer.ElapsedMilliseconds + "ms");

    }


    private void OnDrawGizmosSelected()

    {
        if (!graphCreated)
            return;


        for (int i = 0; i < graph.allNodes.Count; i++)
        {
            for (int j = 0; j < graph.allNodes[i].Count; j++)
            {
                for (int k = 0; k < graph.allNodes[i][j].Count; k++)
                {
                    var currentNode = graph.allNodes[i][j][k];

                    if (currentNode.isWalkable)
                    {
                        //var spanCeiling = currentNode.span.GetSpanCeiling();

                        //Gizmos.color = Color.green;

                        //Gizmos.DrawLine(spanCeiling.bottomLeft, spanCeiling.topLeft);
                        //Gizmos.DrawLine(spanCeiling.topLeft, spanCeiling.topRight);
                        //Gizmos.DrawLine(spanCeiling.topRight, spanCeiling.bottomRight);
                        //Gizmos.DrawLine(spanCeiling.bottomRight, spanCeiling.bottomLeft);

                        //Gizmos.color = Color.red;
                        //foreach(var neighbour in currentNode.NeighbourNodes)
                        //{
                        //    Gizmos.DrawLine(currentNode.GetNodeFloor(), neighbour.GetNodeFloor());
                        //}

                        //draw a cube representing the span

                        var spanBounds = currentNode.span.SpanBounds;
                        Gizmos.color = Color.red;
                        Gizmos.DrawCube(spanBounds.Center, spanBounds.Max - spanBounds.Min);

                        //var spanCeiling = currentNode.span.GetSpanCeiling();

                        //Gizmos.DrawWireSphere(spanCeiling.topLeft, 0.1f);
                        //Gizmos.DrawWireSphere(spanCeiling.topRight, 0.1f);
                        //Gizmos.DrawWireSphere(spanCeiling.bottomLeft, 0.1f);
                        //Gizmos.DrawWireSphere(spanCeiling.bottomRight, 0.1f);


                    }
                }
            }
        }

        //foreach (var node in graph.boundaryNodes)
        //{
        //    var spanBounds = node.span.SpanBounds;
        //    Gizmos.color = Color.red;
        //    Gizmos.DrawCube(spanBounds.Center, spanBounds.Max - spanBounds.Min);
        //}
    }
}