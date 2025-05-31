using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utility.ObjectPool
{
    [Serializable]
    public class PoolData
    {
        public string Name;
        public GameObject Prefab;
    }
    
    [CreateAssetMenu(fileName = "ObjectPoolData", menuName = "Utility/Object Pool Data")]
    public class ObjectPoolData : ScriptableObject
    {
        public List<PoolData> PoolData;
    }
}
