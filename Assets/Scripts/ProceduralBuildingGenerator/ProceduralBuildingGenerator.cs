using System.Collections.Generic;
using UnityEngine;

namespace ProceduralBuildingGenerator
{
    public static class ProceduralBuildingGenerator
    {
        private static readonly List<Context> ChildContexts = new();

        public static GameObject Generate(float buildingHeight, List<Vector3> buildingPositions, BuildingRuleData buildingRuleData)
        {
            ChildContexts.Clear();
            
            GameObject buildingParent = new("building");

            CreateFacade(buildingHeight, buildingPositions, buildingRuleData);

            foreach (Context facade in ChildContexts)
            {
                GameObject facadeObject = new("facade");
                facadeObject.transform.SetParent(buildingParent.transform);
                facade.CreatePrimitive(facadeObject);
            }

            return buildingParent;
        }

        private static void CreateFacade(float height, List<Vector3> corners, BuildingRuleData buildingRuleData)
        {
            switch (corners.Count)
            {
                case <= 1:
                    return;
                case 2:
                    ChildContexts.Add(CreateFacadeWithCorner(height, corners[0], corners[1], buildingRuleData.RootRule));
                    break;
                default:
                {
                    for (int i = 0; i < corners.Count; i++)
                    {
                        Vector3 t1 = corners[i];
                        Vector3 t2 = corners.Count <= i + 1 ? corners[0] : corners[i + 1];

                        ChildContexts.Add(CreateFacadeWithCorner(height, t1, t2, buildingRuleData.RootRule));
                    }

                    break;
                }
            }

            foreach (var corner in corners)
            {
                Context cornerContext = new()
                {
                    Position = corner,
                    Rotation = Quaternion.identity,
                    Size = new Vector2(1.0f, height),
                    RelativeSize = new Vector2(1.0f, height)
                };

                cornerContext.ParseRule(buildingRuleData.CornerRule.ChildRules);
                ChildContexts.Add(cornerContext);
            }
        }

        private static Context CreateFacadeWithCorner(float height, Vector3 t1, Vector3 t2, Rule facadeRule)
        {
            float signedAngleY = Vector3.SignedAngle(-Vector3.right, t1 - t2, Vector3.up);
            Quaternion rotationY = Quaternion.Euler(0.0f, signedAngleY, 0.0f);
            Vector2 size = new(Vector3.Distance(t1, t2), height);
            
            Context facade = new()
            {
                Position = t1,
                Rotation = rotationY,
                Size = size,
                RelativeSize = size
            };

            facade.ParseRule(facadeRule.ChildRules);

            return facade;
        }
    }
}

