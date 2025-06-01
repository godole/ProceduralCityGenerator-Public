using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace ProceduralBuildingGenerator
{
    [System.Serializable]
    public class Rule
    {
        public string Name;
        public string Concat;
        public string Argument;
        public List<Rule> ChildRules = new();
    }
    
    [CreateAssetMenu(fileName = "BuildingRuleData", menuName = "ScriptableObjects/BuildingRuleData")]
    public class BuildingRuleData : ScriptableObject
    {
        [FormerlySerializedAs("_rootRule")] [SerializeField] public Rule RootRule;
        [FormerlySerializedAs("_cornerRule")] [SerializeField] public Rule CornerRule;
    }
}


