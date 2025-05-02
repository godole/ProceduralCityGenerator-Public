using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MathTester : MonoBehaviour
{
    [SerializeField] private GameObject _CircleCenterObject1;
    [SerializeField] private GameObject _CircleCenterObject2;

    [SerializeField] private GameObject _resultObject;
    //[SerializeField] private LineRenderer _resultRenderer;

    private void Start()
    {
    }


    // Update is called once per frame
    void Update()
    {
        float dirRotation = GetDirRotation(_CircleCenterObject1.transform.position, _CircleCenterObject2.transform.position);
        
        _resultObject.transform.position = _CircleCenterObject1.transform.position;
        _resultObject.transform.rotation = Quaternion.Euler(0.0f, -dirRotation, 0.0f);
        _resultObject.transform.localScale =
            new Vector3(Vector3.Distance(_CircleCenterObject1.transform.position, _CircleCenterObject2.transform.position), 1.0f, 1.0f);
    }
    
    float GetDirRotation(Vector3 startPos, Vector3 endPos)
    {
        Vector3 dir =  endPos - startPos;
        return Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg;
    }
}
