using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace ProceduralBuildingGenerator
{
    [CreateAssetMenu(fileName = "BuildingRuleData", menuName = "ScriptableObjects/BuildingRuleData")]
    public class BuildingRuleData : ScriptableObject
    {
        [SerializeField] public List<ObjectPoolContainer.PoolData> _poolData;

        private GameObject _rootObject;
        [SerializeField] public Rule _rootRule;
        [SerializeField] public Rule _cornerRule;

        public void Generate()
        {
            ObjectPoolContainer.Instance.InitWithPoolData(_buildingRuleData._poolDatas);
            ObjectPoolContainer.Instance.ResetAll();
        
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

        public class Mass
        {
            public enum PrimitiveType
            {
                eRectangle,
                eTrident,
                eHexagon
            }

            public class PrimitiveData
            {
                public PrimitiveType Type;
                public float Radius;
                public int CornerCount;
                public Vector3 Size;
                public Vector3 Position;
            }

            public Vector3 Position;
            public Vector3 Size;
            public Rule FacadeRule;
            public Rule CornerRule;
            public List<Context> _childContexts = new List<Context>();

            public void CreateWithPrimitiveData(PrimitiveData data)
            {
                float height = data.Size.y;
                List<Vector3> corners = new List<Vector3>();

                Position = data.Position;
                Size = data.Size;

                if (data.Type == PrimitiveType.eRectangle)
                {
                    Vector3 halfSize = Size * 0.5f;
                    corners.Add(new Vector3(Position.x - halfSize.x, Position.y, Position.z - halfSize.z));

                    corners.Add(new Vector3(Position.x - halfSize.x, Position.y, Position.z + halfSize.z));

                    corners.Add(new Vector3(Position.x + halfSize.x, Position.y, Position.z + halfSize.z));
                    corners.Add(new Vector3(Position.x + halfSize.x, Position.y, Position.z - halfSize.z));
                }

                else
                {
                    float currentAngle = 0.0f;
                    float deltaAngle = 360.0f / data.CornerCount;

                    while (currentAngle <= Mathf.PI * 2.0f)
                    {
                        Vector3 angleCornerPosition = new Vector3(Mathf.Sin(currentAngle) * data.Radius, 0.0f,
                            Mathf.Cos(currentAngle) * data.Radius) + Position;
                        corners.Add(angleCornerPosition);
                        currentAngle += Mathf.Deg2Rad * deltaAngle;
                    }
                }

                CreateFacade(height, corners);
            }

            public void CreateFacade(float height, List<Vector3> corners)
            {
                if (corners.Count <= 1)
                {
                    return;
                }

                else if (corners.Count == 2)
                {
                    _childContexts.Add(CreateFacadeWithCorner(height, corners[0], corners[1]));
                }

                else
                {
                    for (int i = 0; i < corners.Count; i++)
                    {
                        Vector3 t1 = corners[i];
                        Vector3 t2 = Vector3.zero;

                        if (corners.Count <= i + 1)
                        {
                            t2 = corners[0];
                        }
                        else
                        {
                            t2 = corners[i + 1];
                        }

                        _childContexts.Add(CreateFacadeWithCorner(height, t1, t2));
                    }
                }

                foreach (var corner in corners)
                {
                    var cornerContext = new Context()
                    {
                        Position = corner,
                        Rotation = Quaternion.identity,
                        Size = new Vector2(1.0f, height),
                        RelativeSize = new Vector2(1.0f, height)
                    };

                    cornerContext.ParseRule(CornerRule.ChildRules);
                    _childContexts.Add(cornerContext);
                }
            }

            private Context CreateFacadeWithCorner(float height, Vector3 t1, Vector3 t2)
            {
                float signedAngleY = Vector3.SignedAngle(-Vector3.right, t1 - t2, Vector3.up);
                Quaternion rotationY = Quaternion.Euler(0.0f, signedAngleY, 0.0f);

                return CreateFacadeInternal(t1, new Vector2(Vector3.Distance(t1, t2), height), rotationY);
            }

            private Context CreateFacadeInternal(Vector3 Pos, Vector2 Size, Quaternion Rotation)
            {
                var facade = new Context()
                {
                    Position = Pos,
                    Rotation = Rotation,
                    Size = Size,
                    RelativeSize = Size
                };

                facade.ParseRule(FacadeRule.ChildRules);

                return facade;
            }
        }
    }
}


