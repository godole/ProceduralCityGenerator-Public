using System.Collections.Generic;
using UnityEngine;

namespace ProceduralBuildingGenerator
{
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

            mass.CreateFacade(_buildingHeight, _buildingPoints);
            
            foreach (ProceduralBuildingGenerator.Context facade in mass._childContexts)
            {
                facade.CreatePrimitive(new GameObject());
            }
        }
    }
}
