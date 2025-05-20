using System.Collections.Generic;
using Scenes;
using UnityEngine;

public class MathUtilTester : MonoBehaviour
{
    [SerializeField] public List<Transform> polygonPoints;
    [SerializeField] public LineRenderer lineRenderer;
    [SerializeField] public float shrinkDistance;
    
    private List<Vector3> polygonPointPositions = new List<Vector3>();
    

    private void Start()
    {
        foreach (var polygonPoint in polygonPoints)
        {
            polygonPointPositions.Add(polygonPoint.position);
        }
        
        TestUtil.CreateWireframePolygonObject(lineRenderer, polygonPointPositions);
        TestUtil.CreateWireframePolygonObject(lineRenderer, PolygonSplit.GetShrinkPolygon(polygonPointPositions, shrinkDistance));
    }
}
