using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "CellPrefabs", menuName = "PRG/CellPrefabs")]
public class CellPrefab : ScriptableObject
{
    [FormerlySerializedAs("Prefabs")] public GameObject[] prefabs;
    public bool[,] Rules;
}
