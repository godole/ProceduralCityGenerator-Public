using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Utility.ObjectPool;

namespace ProceduralBuildingGenerator
{
    class MeshCombineData
    {
        public Material Material;
        public List<GameObject> Objects;
    }
    
    public class BuildingGeneratorTest : MonoBehaviour
    {
        [SerializeField] private ObjectPoolData BuildingPoolData;
        [FormerlySerializedAs("_buildingRuleData")] [SerializeField] private BuildingRuleData BuildingRuleData;
        [FormerlySerializedAs("_buildingPointParent")] [SerializeField] private Transform BuildingPointParent;
        [FormerlySerializedAs("_buildingHeight")] [SerializeField] private float BuildingHeight;
    
        private readonly List<Vector3> _buildingPoints = new();
    
        private void Start()
        {
            ObjectPoolContainer.Instance.InitWithPoolData(BuildingPoolData);    
            
            for (int i = 0; i < BuildingPointParent.childCount; i++)
            {
                _buildingPoints.Add(BuildingPointParent.GetChild(i).position);
            }
        
            ProceduralBuildingGenerator.Generate(BuildingHeight, _buildingPoints, BuildingRuleData);
        }
    }
}
