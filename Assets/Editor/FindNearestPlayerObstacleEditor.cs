using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor (typeof (FindNearestPlayerObstacle))]
public class FindNearestPlayerObstacleEditor : Editor
{
    private void OnSceneGUI()
    {
        FindNearestPlayerObstacle fow = (FindNearestPlayerObstacle)target;
        Handles.color = Color.black;
        Handles.DrawWireArc(fow.transform.position, Vector3.up, Vector3.forward, 360, fow.searchRadius);
    }
}
