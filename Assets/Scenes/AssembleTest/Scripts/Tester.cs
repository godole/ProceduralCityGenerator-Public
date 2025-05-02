using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tester : MonoBehaviour
{
    [SerializeField] private int _randomSeed;
    // Start is called before the first frame update
    void Awake()
    {
        Random.InitState(_randomSeed);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
