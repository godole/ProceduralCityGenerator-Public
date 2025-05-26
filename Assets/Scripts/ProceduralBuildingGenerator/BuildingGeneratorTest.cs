using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingGeneratorTest : MonoBehaviour
{
    [SerializeField] private ProceduralBuildingGenerator _buildingGenerator;
    [SerializeField] private Transform _buildingPointParent;
    [SerializeField] private float _buildingHeight;
    
    private List<Vector3> _buildingPoints = new List<Vector3>();
    
    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < _buildingPointParent.childCount; i++)
        {
            _buildingPoints.Add(_buildingPointParent.GetChild(i).position);
        }
        
        ObjectPoolContainer.Instance.InitWithPoolData(_buildingGenerator._poolDatas);
        ObjectPoolContainer.Instance.ResetAll();
        
        ProceduralBuildingGenerator.Mass mass = new ProceduralBuildingGenerator.Mass();

        mass.FacadeRule = _buildingGenerator._rootRule;
        mass.CornerRule = _buildingGenerator._cornerRule;
            
        // mass.CreateWithPrimitiveData(new ProceduralBuildingGenerator.Mass.PrimitiveData
        // {
        //     CornerCount = 8,
        //     Position = Vector3.zero,
        //     Radius = 20.0f,
        //     Size = new Vector3(20.0f, 50.0f, 20.0f),
        //     Type = ProceduralBuildingGenerator.Mass.PrimitiveType.eRectangle
        // });
        
        mass.CreateFacade(_buildingHeight, _buildingPoints);
            
        foreach (var facade in mass._childContexts)
        {
            facade.CreatePrimitive(new GameObject());
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
