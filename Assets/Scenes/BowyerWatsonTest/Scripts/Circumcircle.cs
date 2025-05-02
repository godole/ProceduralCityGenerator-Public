using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Circumcircle : MonoBehaviour
{
    [SerializeField] Transform[] points;
    [SerializeField] GameObject r;
    // Start is called before the first frame update
    void Start()
    {
        Triangle c = new Triangle();

        c.P1 = points[0].position;
        c.P2 = points[1].position;
        c.P3 = points[2].position;

        c.Initialize();

        r.transform.position = c.Circumcircle.Center;
        r.transform.localScale = new Vector3(c.Circumcircle.Radius * 2, 0.1f, c.Circumcircle.Radius * 2);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
