using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RegionAnalyzer))]
public class RegionAnalysisEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if(GUILayout.Button("Create Graph"))
        {
            var hf = (target as RegionAnalyzer).gameObject.GetComponent<VoxelizeScene>().GetHeightfield();
            (target as RegionAnalyzer).gameObject.GetComponent<VoxelizeScene>().sceneVoxed = false;
            (target as RegionAnalyzer).CreateSpanGraphFromHeightfield(hf);
            (target as RegionAnalyzer).graphCreated = true;
        }
    }
}
