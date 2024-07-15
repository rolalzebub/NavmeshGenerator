using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.NavMesh.RegionAnalysis
{
    /// <summary>
    /// Collection of regions representing the entire scene
    /// </summary>
    public class RegionSpanGraph
    {
        AgentSettings settings;

        List<RegionSpan> regions;



    }


    /// <summary>
    /// Collection of vertical spans where all spans are connected to each other via neighbours or directly i.e. form a region
    /// </summary>
    public class RegionSpan
    {
        List<VerticalSpan> connectedSpans = new List<VerticalSpan>();

        //NaN by default, then it gets a height when the first span is added to the collection
        //consider it a way of initialization I guess?
        float regionCeiling = float.NaN;

        //get the vertical level of the region
        public float GetRegionHeight()
        {
            return regionCeiling;
        }

        public void AddSpanToRegion(VerticalSpan span)
        {
            connectedSpans.Add(span);

            //in case the span pushes the ceiling higher, update the stored ceiling value
            if (span.SpanBounds.Max.y > regionCeiling)
            {
                regionCeiling = span.SpanBounds.Max.y;
            }
        }
    }
}
