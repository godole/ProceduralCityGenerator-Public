using System.Collections.Generic;
using Scenes;
using UnityEngine;
using Utility;

namespace MathUtilTester
{
    public class MathUtilTester : MonoBehaviour
    {
        [SerializeField] private bool useManualData;
        [SerializeField] private GameObject positionParent;
        [SerializeField] private TestBuildingPositionData positionData;
        [SerializeField] public GameObject polygonPrefab;
        [SerializeField] public LineRenderer lineRenderer;
        [SerializeField] public float shrinkDistance;
    
        private List<Vector3> polygonPointPositions = new List<Vector3>();

        private List<LineRenderer> existLineRendererInstances = new List<LineRenderer>();
        private int currentActiveInstanceCount = 0;

        private void Start()
        {
            currentActiveInstanceCount = 0;
        
            if (useManualData)
            {
                for (int i = 0; i < positionParent.transform.childCount; i++)
                {
                    var child = positionParent.transform.GetChild(i);
                    var positionInstance = Instantiate(polygonPrefab);
                    positionInstance.transform.position = child.position;
                    polygonPointPositions.Add(child.position);
                }
            
                //polygonPointPositions.Add(positionParent.transform.GetChild(0).position);
            }
            else
            {
                foreach (var polygonPoint in positionData.Positions)
                {
                    var positionInstance = Instantiate(polygonPrefab);
                    positionInstance.transform.position = polygonPoint;
                    polygonPointPositions.Add(polygonPoint);
                }
            }
        
            CleanUpInstance();
        
            var shrinkPolygons = PolygonSplit.GetShrinkPolygon(polygonPointPositions, shrinkDistance);
        
            foreach (var shrinkPolygon in shrinkPolygons)
            {
                var instance = GetInstance(shrinkPolygon);
            }
        }

        private void OnValidate()
        {
            if (polygonPointPositions.Count == 0)
            {
                return;
            }
        
            CleanUpInstance();
        
            var shrinkPolygons = PolygonSplit.GetShrinkPolygon(polygonPointPositions, shrinkDistance);
        
            foreach (var shrinkPolygon in shrinkPolygons)
            {
                var instance = GetInstance(shrinkPolygon);
            }
        }

        private LineRenderer GetInstance(List<Vector3> polygonPoints)
        {
            if (currentActiveInstanceCount <= existLineRendererInstances.Count)
            {
                var newInstance = TestUtil.CreateWireframePolygonObject(lineRenderer, polygonPoints);
                var newLineRenderer = newInstance.GetComponent<LineRenderer>();
                existLineRendererInstances.Add(newInstance.GetComponent<LineRenderer>());
                return newLineRenderer;
            }
        
            var existInstance = existLineRendererInstances[currentActiveInstanceCount];
            existInstance.gameObject.SetActive(true);
            currentActiveInstanceCount++;
            return existInstance;
        }

        private void CleanUpInstance()
        {
            foreach (var existLineRendererInstance in existLineRendererInstances)
            {
                existLineRendererInstance.gameObject.SetActive(false);
            }
        }
    }
}
