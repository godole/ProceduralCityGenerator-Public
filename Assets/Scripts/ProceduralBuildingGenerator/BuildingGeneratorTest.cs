using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace ProceduralBuildingGenerator
{
    class MeshCombineData
    {
        public Material Material;
        public List<GameObject> Objects;
    }
    
    public class BuildingGeneratorTest : MonoBehaviour
    {
        [SerializeField] private BuildingRuleData _buildingRuleData;
        [SerializeField] private Transform _buildingPointParent;
        [SerializeField] private float _buildingHeight;
    
        private readonly List<Vector3> _buildingPoints = new();
    
        private void Start()
        {
            for (int i = 0; i < _buildingPointParent.childCount; i++)
            {
                _buildingPoints.Add(_buildingPointParent.GetChild(i).position);
            }
        
            BuildingRuleData.ObjectPoolContainer.Instance.InitWithPoolData(_buildingRuleData._poolData);
            BuildingRuleData.ObjectPoolContainer.Instance.ResetAll();
        
            BuildingRuleData.Mass mass = new()
            {
                FacadeRule = _buildingRuleData._rootRule,
                CornerRule = _buildingRuleData._cornerRule
            };

            GameObject buildingParent = new GameObject("building");

            mass.CreateFacade(_buildingHeight, _buildingPoints);
            
            foreach (BuildingRuleData.Context facade in mass._childContexts)
            {
                var facadeObject = new GameObject("facade");
                facadeObject.transform.SetParent(buildingParent.transform);
                facade.CreatePrimitive(facadeObject);
            }
        }
    }
}
