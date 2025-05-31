using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool
{
    public GameObject Prefab;

    private List<GameObject> _ActiveObjectList = new List<GameObject>();
    private Queue<GameObject> _InactiveObjects = new Queue<GameObject>();

    public ObjectPool(GameObject prefab)
    {
        Prefab = prefab;
    }

    public GameObject Get()
    {
        if (_InactiveObjects.Count == 0)
        {
            GameObject newObject = GameObject.Instantiate(Prefab);
            _ActiveObjectList.Add(newObject);
            return newObject;
        }
        else
        {
            var retObj = _InactiveObjects.Dequeue();
            _ActiveObjectList.Add(retObj);
            retObj.SetActive(true);
            return retObj;
        }
    }

    public void Pool(GameObject obj)
    {
        obj.SetActive(false);
        _InactiveObjects.Enqueue(obj);
    }

    public void ResetAll()
    {
        foreach (var activeObject in _ActiveObjectList)
        {
            Pool(activeObject);
        }

        _ActiveObjectList.Clear();
    }
}

public class ObjectPoolContainer
{
    [Serializable]
    public class PoolData
    {
        public string Name;
        public GameObject Prefab;
    }

    private static ObjectPoolContainer _instance = null;

    private Dictionary<string, ObjectPool> _objectPools = new Dictionary<string, ObjectPool>();

    public static ObjectPoolContainer Instance
    {
        get
        {
            if (_instance != null) return _instance;
            _instance = new ObjectPoolContainer();

            return _instance;
        }
    }

    public void InitWithPoolData(List<PoolData> data)
    {
        foreach (var d in data)
        {
            var pool = new ObjectPool(d.Prefab);

            _objectPools.Add(d.Name, pool);
        }
    }

    public ObjectPool GetPool(string name)
    {
        return _objectPools[name];
    }

    public void ResetAll()
    {
        foreach (var pool in _objectPools)
        {
            pool.Value.ResetAll();
        }
    }
}
