using EarClipperLib;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpanGraph
{
    public SpanGraphNode rootNode;
    AgentSettings settings;


    //xz-y ordered list of spans
    public List<List<List<SpanGraphNode>>> allNodes;

    public List<SpanGraphNode> boundaryNodes = new List<SpanGraphNode>();

    public SpanGraph(SpanGraphNode _rootNode, AgentSettings _settings)
    {
        rootNode = _rootNode;

        allNodes = new List<List<List<SpanGraphNode>>>
        {
            new List<List<SpanGraphNode>>()
        };

        allNodes[0].Add(new List<SpanGraphNode>());
        allNodes[0][0].Add(rootNode);

        settings = _settings;
    }

    public void FindAllBoundaryNodes()
    {
        for (int xIndex = 0; xIndex < allNodes.Count; xIndex++)
        {
            for (int zIndex = 0; zIndex < allNodes[xIndex].Count; zIndex++)
            {
                for (int yIndex = 0; yIndex < allNodes[xIndex][zIndex].Count; yIndex++)
                {
                    var node = allNodes[xIndex][zIndex][yIndex];

                    //ignore non-walkable nodes
                    if (!node.isWalkable)
                    {
                        continue;
                    }

                    //if it has all neighbours, it can't possibly be a boundary node
                    if (node.Neighbours.HasAllNeighbours())
                    {
                        continue;
                    }
                    //check if it has any neighbours at all otherwise it is unreachable
                    else if (node.Neighbours.HasAnyNeighbours())
                    {
                        boundaryNodes.Add(node);
                    }
                }
            }
        }
    }

    public void CreatePolygonFromBoundaryNodes()
    {
        //multiply by 4 because each element will have 4 verts
        List<Vector3m> boundaryVerts = new List<Vector3m>();

        foreach(var node in boundaryNodes)
        {
            var nodeCeiling = node.span.GetSpanCeiling();

            Vector3m topLeft = new Vector3m(nodeCeiling.topLeft.x, nodeCeiling.topLeft.y, nodeCeiling.topLeft.z);
            Vector3m topRight = new Vector3m(nodeCeiling.topRight.x, nodeCeiling.topRight.y, nodeCeiling.topRight.z);
            Vector3m bottomRight = new Vector3m(nodeCeiling.bottomRight.x, nodeCeiling.bottomRight.y, nodeCeiling.bottomRight.z);
            Vector3m bottomLeft = new Vector3m(nodeCeiling.bottomLeft.x, nodeCeiling.bottomLeft.y, nodeCeiling.bottomLeft.z);

            boundaryVerts.Add(topLeft);
            boundaryVerts.Add(topRight);
            boundaryVerts.Add(bottomRight);
            boundaryVerts.Add(bottomLeft);
        }

        EarClipping earClipping = new EarClipping();
        earClipping.SetPoints(boundaryVerts);
        earClipping.Triangulate();

        var result = earClipping.Result;
    }

    public void MakeAllNeighbourConnections()
    {
        for (int i = 0; i < allNodes.Count; i++)
        {
            for (int j = 0; j < allNodes[i].Count; j++)
            {
                for (int k = 0; k < allNodes[i][j].Count; k++)
                {
                    var currentNode = allNodes[i][j][k];

                    if (!currentNode.isWalkable)
                    {
                        continue;
                    }

                    ConnectToNeighbours(ref currentNode, i, j, k);

                    allNodes[i][j][k] = currentNode;
                }
            }
        }
    }

    void ConnectToNeighbours(ref SpanGraphNode node, int xIndex, int zIndex, int yIndex)
    {
        //ignore nodes that are not walkable
        if(!node.isWalkable)
        {
            return;
        }

        //neighbours be like: [x,z]
        //[-1,1] , [0,1] , [1,1]
        //[-1,0] , [0,0] , [0,1]
        //[-1,-1] , [0,-1] , [1,-1]
        NeighbourNodes neighbours = new NeighbourNodes();

        //if there is any space to the left
        if (xIndex > 0)
        {
            //centre left neighbour
            foreach(var neighbourNode in allNodes[xIndex - 1][zIndex])
            {
                //ignore if it is empty space
                if (!neighbourNode.isNodeEmpty)
                {
                    //ignore if it is not walkable
                    if(neighbourNode.isWalkable)
                    {
                        //check if vertical displacement between platforms is within range of one step of agent's vertical step distance
                        if(Mathf.Abs(neighbourNode.span.GetSpanCeilingLevel() - node.span.GetSpanCeilingLevel()) <= settings.maxStepHeight)
                        {
                            neighbours.centreLeftNeighbour = neighbourNode;
                            break;
                        }
                    }
                }
            }


            //if there is any space ahead
            if (zIndex < allNodes[xIndex].Count - 1)
            {
                //forward left neighbour
                foreach (var neighbourNode in allNodes[xIndex - 1][zIndex + 1])
                {
                    //ignore if it is empty space
                    if (!neighbourNode.isNodeEmpty)
                    {
                        //ignore if it is not walkable
                        if (neighbourNode.isWalkable)
                        {
                            //check if vertical displacement between platforms is within range of one step of agent's vertical step distance
                            if (Mathf.Abs(neighbourNode.span.GetSpanCeilingLevel() - node.span.GetSpanCeilingLevel()) <= settings.maxStepHeight)
                            {
                                neighbours.forwardLeftNeighbour = neighbourNode;
                                break;
                            }
                        }
                    }
                }

                //forward neighbour
                foreach (var neighbourNode in allNodes[xIndex][zIndex + 1])
                {
                    //ignore if it is empty space
                    if (!neighbourNode.isNodeEmpty)
                    {
                        //ignore if it is not walkable
                        if (neighbourNode.isWalkable)
                        {
                            //check if vertical displacement between platforms is within range of one step of agent's vertical step distance
                            if (Mathf.Abs(neighbourNode.span.GetSpanCeilingLevel() - node.span.GetSpanCeilingLevel()) <= settings.maxStepHeight)
                            {
                                neighbours.forwardNeighbour = neighbourNode;
                                break;
                            }
                        }
                    }
                }
            }

            //if there is any space behind
            if(zIndex > 0)
            {
                //back left neighbour
                foreach (var neighbourNode in allNodes[xIndex - 1][zIndex - 1])
                {
                    //ignore if it is empty space
                    if (!neighbourNode.isNodeEmpty)
                    {
                        //ignore if it is not walkable
                        if (neighbourNode.isWalkable)
                        {
                            //check if vertical displacement between platforms is within range of one step of agent's vertical step distance
                            if (Mathf.Abs(neighbourNode.span.GetSpanCeilingLevel() - node.span.GetSpanCeilingLevel()) <= settings.maxStepHeight)
                            {
                                neighbours.backLeftNeighbour = neighbourNode;
                                break;
                            }
                        }
                    }
                }

                //back neighbour
                foreach (var neighbourNode in allNodes[xIndex][zIndex - 1])
                {
                    //ignore if it is empty space
                    if (!neighbourNode.isNodeEmpty)
                    {
                        //ignore if it is not walkable
                        if (neighbourNode.isWalkable)
                        {
                            //check if vertical displacement between platforms is within range of one step of agent's vertical step distance
                            if (Mathf.Abs(neighbourNode.span.GetSpanCeilingLevel() - node.span.GetSpanCeilingLevel()) <= settings.maxStepHeight)
                            {
                                neighbours.backNeighbour = neighbourNode;
                                break;
                            }
                        }
                    }
                }
            }
        }

        //if there is any space to the right
        if (xIndex < allNodes.Count - 1)
        {
            //centre right neighbour
            foreach (var neighbourNode in allNodes[xIndex + 1][zIndex])
            {
                //ignore if it is empty space
                if (!neighbourNode.isNodeEmpty)
                {
                    //ignore if it is not walkable
                    if (neighbourNode.isWalkable)
                    {
                        //check if vertical displacement between platforms is within range of one step of agent's vertical step distance
                        if (Mathf.Abs(neighbourNode.span.GetSpanCeilingLevel() - node.span.GetSpanCeilingLevel()) <= settings.maxStepHeight)
                        {
                            neighbours.centreRightNeighbour = neighbourNode;
                            break;
                        }
                    }
                }
            }

            //if there is any space ahead
            if (zIndex < allNodes[xIndex].Count - 1)
            {
                //forward right neighbour
                foreach (var neighbourNode in allNodes[xIndex + 1][zIndex + 1])
                {
                    //ignore if it is empty space
                    if (!neighbourNode.isNodeEmpty)
                    {
                        //ignore if it is not walkable
                        if (neighbourNode.isWalkable)
                        {
                            //check if vertical displacement between platforms is within range of one step of agent's vertical step distance
                            if (Mathf.Abs(neighbourNode.span.GetSpanCeilingLevel() - node.span.GetSpanCeilingLevel()) <= settings.maxStepHeight)
                            {
                                neighbours.forwardRightNeighbour = neighbourNode;
                                break;
                            }
                        }
                    }
                }
            }

            //if there is any space behind
            if (zIndex > 0)
            {
                //back right neighbour
                foreach (var neighbourNode in allNodes[xIndex + 1][zIndex - 1])
                {
                    //ignore if it is not walkable
                    if (neighbourNode.isWalkable)
                    {
                        //check if vertical displacement between platforms is within range of one step of agent's vertical step distance
                        if (Mathf.Abs(neighbourNode.span.GetSpanCeilingLevel() - node.span.GetSpanCeilingLevel()) <= settings.maxStepHeight)
                        {
                            neighbours.backRightNeighbour = neighbourNode;
                            break;
                        }
                    }
                }
            }
        }
        
        node.Neighbours = neighbours;
    }


    public class SpanGraphNode
    {
        public bool isWalkable;

        public VerticalSpan span;

        public List<SpanGraphNode> NeighbourNodes;

        public NeighbourNodes Neighbours;

        public bool isNodeEmpty = true;

        public SpanGraphNode(VerticalSpan _span)
        {
            span = _span;
            NeighbourNodes = new List<SpanGraphNode>();
            isNodeEmpty = false;
        }

        public void AddNeighbour(SpanGraphNode newNeighbour)
        {
            if (NeighbourNodes == null)
            {
                NeighbourNodes = new List<SpanGraphNode>();
            }

            NeighbourNodes.Add(newNeighbour);
        }


        /// <summary>
        /// returns a vector3 that is the middle of the floorspace covered by this span
        /// </summary>
        /// <returns></returns>
        public Vector3 GetNodeFloor()
        {
            var spanFloor = span.GetSpanFloor();

            Vector3 nodePosition = spanFloor.topRight - spanFloor.bottomLeft;

            return nodePosition;
        }
    }

    public struct SpanGraphNodeQuad
    {
        public Vector3 bottomLeft, topLeft, topRight, bottomRight;
        public Vector3 quadNormal;
    }

}

/// <summary>
/// This is a collection of nodes that can surround a span graph node
/// </summary>
public class NeighbourNodes
{
    public SpanGraph.SpanGraphNode forwardNeighbour = null;
    public SpanGraph.SpanGraphNode forwardLeftNeighbour = null;
    public SpanGraph.SpanGraphNode forwardRightNeighbour = null;

    public SpanGraph.SpanGraphNode centreLeftNeighbour = null;
    public SpanGraph.SpanGraphNode centreRightNeighbour = null;

    public SpanGraph.SpanGraphNode backNeighbour = null;
    public SpanGraph.SpanGraphNode backLeftNeighbour = null;
    public SpanGraph.SpanGraphNode backRightNeighbour = null;

    public NeighbourNodes()
    {
        forwardNeighbour = null;
        forwardLeftNeighbour = null;
        forwardRightNeighbour = null;

        centreLeftNeighbour = null;
        centreRightNeighbour = null;

        backNeighbour = null;
        backLeftNeighbour = null;
        backRightNeighbour = null;
    }

    public bool HasAllNeighbours()
    {
        if (forwardNeighbour == null) { return false; }

        if (forwardLeftNeighbour == null) { return false; }

        if (forwardRightNeighbour == null) { return false; }

        if (centreLeftNeighbour == null) { return false; }

        if (centreRightNeighbour == null) { return false; }

        if (backLeftNeighbour == null) { return false; }

        if (backNeighbour == null) { return false; }

        if (backRightNeighbour == null) { return false; }

        return true;
    }

    public bool HasAnyNeighbours()
    {
        if (forwardNeighbour != null) { return true; }

        if (forwardLeftNeighbour != null) { return true; }

        if (forwardRightNeighbour != null) { return true; }

        if (centreLeftNeighbour != null) { return true; }

        if (centreRightNeighbour != null) { return true; }

        if (backLeftNeighbour != null) { return true; }

        if (backNeighbour != null) { return true; }

        if (backRightNeighbour != null) { return true; }

        return false;
    }
}

public struct AgentSettings
{
    public float maxStepDistance;
    public float agentHeight;
    public float agentRadius;
    public float maxStepHeight;
    public float maxWalkableAngle;
}

public class WalkableSpanField
{
    public List<List<List<SpanGraph.SpanGraphNode>>> allNodes = new List<List<List<SpanGraph.SpanGraphNode>>>();

    public NeighbourNodes neigbours = new NeighbourNodes();

    SpanGraph.SpanGraphNode rootNode;

    public WalkableSpanField(SpanGraph.SpanGraphNode _rootNode)
    {
        rootNode = _rootNode;

        allNodes = new List<List<List<SpanGraph.SpanGraphNode>>>
        {
            new List<List<SpanGraph.SpanGraphNode>>()
        };

        allNodes[0].Add(new List<SpanGraph.SpanGraphNode>());
        allNodes[0][0].Add(rootNode);

    }
}