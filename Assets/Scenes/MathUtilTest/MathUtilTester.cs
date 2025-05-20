using System;
using System.Collections.Generic;
using Scenes;
using UnityEngine;

public class MathUtilTester : MonoBehaviour
{
    [SerializeField] private TestBuildingPositionData positionData;
    [SerializeField] public GameObject polygonPrefab;
    [SerializeField] public LineRenderer lineRenderer;
    [SerializeField] public float shrinkDistance;
    
    private List<Vector3> polygonPointPositions = new List<Vector3>();

    private LineRenderer existLineRendererInstance;

    private void Start()
    {
        foreach (var polygonPoint in positionData.Positions)
        {
            var positionInstance = Instantiate(polygonPrefab);
            positionInstance.transform.position = polygonPoint;
            polygonPointPositions.Add(polygonPoint);
        }
        
        TestUtil.CreateWireframePolygonObject(lineRenderer, polygonPointPositions);
        var testInstanceObject = TestUtil.CreateWireframePolygonObject(lineRenderer, PolygonSplit.GetShrinkPolygon(polygonPointPositions, shrinkDistance));
        existLineRendererInstance = testInstanceObject.GetComponent<LineRenderer>();
    }

    private void OnValidate()
    {
        if (existLineRendererInstance == null)
        {
            return;
        }
        
        var shrinkPolygon = PolygonSplit.GetShrinkPolygon(polygonPointPositions, shrinkDistance);
        shrinkPolygon.Add(shrinkPolygon[0]);

        existLineRendererInstance.positionCount = shrinkPolygon.Count;
        existLineRendererInstance.SetPositions(shrinkPolygon.ToArray());
    }
}
