using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class SPTester : MonoBehaviour
{
    [SerializeField] private GameObject _testLineRendererPrefab;

    // Start is called before the first frame update
    void Start()
    {
        List<Vector3> inputs = new List<Vector3>();

        for (int i = 0; i < transform.childCount; i++)
        {
            Vector3 current = transform.GetChild(i).position;
            Vector3 next = transform.GetChild(i + 1 >= transform.childCount ? 0 : i + 1).position;
            DrawTestLine(current, next);
            inputs.Add(current);
        }

        StraightPolygon straightPolygon = new StraightPolygon();

        var resultEdges = straightPolygon.Process(inputs); 
        
        foreach (var result in resultEdges)
        {
            DrawTestLine(result.PreviousVertex.Position, result.NextVertex.Position);
        }
    }

    void DrawTestLine(Vector3 P1, Vector3 P2)
    {
        var testObject = Instantiate(_testLineRendererPrefab);
        var lineRenderer = testObject.GetComponent<LineRenderer>();
        lineRenderer.SetPositions(new []{P1, P2});
    }
}
