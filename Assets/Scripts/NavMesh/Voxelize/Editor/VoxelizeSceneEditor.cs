using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.ComponentModel;
using System.Threading.Tasks;

[CustomEditor(typeof(VoxelizeScene))]
public class VoxelizeSceneEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if(GUILayout.Button("Voxelize Scene"))
        {
            (target as VoxelizeScene).VoxelizeSceneByCombiningMeshes();
        }
    }
}
