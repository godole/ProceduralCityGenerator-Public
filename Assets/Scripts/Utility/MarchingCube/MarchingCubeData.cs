using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "PRG/MarchingCubeData", fileName = "MarchingCubeData")]
public class MarchingCubeData : ScriptableObject
{
    [System.Serializable]
    public class EdgePositions
    {
        public List<Vector3> Positions;
    }
    
    public List<GameObject> LookupTable;
    public List<EdgePositions> EdgePositionsList;
}
