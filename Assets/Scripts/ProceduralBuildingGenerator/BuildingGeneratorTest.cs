using System.Collections.Generic;
using UnityEngine;

namespace ProceduralBuildingGenerator
{
    class MeshCombineData
    {
        public Material Material;
        public List<GameObject> Objects;
    }
    
    public class BuildingGeneratorTest : MonoBehaviour
    {
        [SerializeField] private ProceduralBuildingGenerator _buildingGenerator;
        [SerializeField] private Transform _buildingPointParent;
        [SerializeField] private float _buildingHeight;
    
        private readonly List<Vector3> _buildingPoints = new();
    
        private void Start()
        {
            for (int i = 0; i < _buildingPointParent.childCount; i++)
            {
                _buildingPoints.Add(_buildingPointParent.GetChild(i).position);
            }
        
            ObjectPoolContainer.Instance.InitWithPoolData(_buildingGenerator._poolDatas);
            ObjectPoolContainer.Instance.ResetAll();
        
            ProceduralBuildingGenerator.Mass mass = new()
            {
                FacadeRule = _buildingGenerator._rootRule,
                CornerRule = _buildingGenerator._cornerRule
            };

            GameObject buildingParent = new GameObject("building");

            mass.CreateFacade(_buildingHeight, _buildingPoints);
            
            foreach (ProceduralBuildingGenerator.Context facade in mass._childContexts)
            {
                var facadeObject = new GameObject("facade");
                facadeObject.transform.SetParent(buildingParent.transform);
                facade.CreatePrimitive(facadeObject);
            }
            
            
        }
    }
}
