using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;
using Random = System.Random;


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

public class ProceduralBuildingGenerator : MonoBehaviour
{
    public class Context
    {
        public string Name;

        public Vector2 RelativeSize;
        public Vector2 Size;
        public Quaternion Rotation;
        public Vector3 RelativePosition;
        public Vector3 Position;

        public bool IsPrimitive = false;
        public bool UseLocalScale = false;
        public ObjectPool PrimitivePool;
        
        public List<Context> ChildContext = new List<Context>();

        GameObject _attachedObject = null;

        public void ParseRule(List<Rule> rules)
        {
            foreach (var rule in rules)
            {
                if (rule.Concat.Equals("StaticSplitX"))
                {
                    if (!float.TryParse(rule.Argument, out var sizeX))
                    {
                        continue;
                    }
                    
                    _CreateChildContext(new Vector3(0.0f, 0.0f, 0.0f), new Vector2(sizeX, Size.y), Quaternion.identity, rule);

                    RelativeSize.x -= sizeX;
                    RelativePosition.x = sizeX;
                }

                if(rule.Concat.Equals("XStaticSplit"))
                {
                    if (!float.TryParse(rule.Argument, out var sizeX))
                    {
                        continue;
                    }

                    _CreateChildContext(new Vector3(Size.x - sizeX, 0.0f, 0.0f), new Vector2(sizeX, Size.y), Quaternion.identity, rule);

                    RelativeSize.x -= sizeX;
                }

                if (rule.Concat.Equals("StaticSplitY"))
                {
                    if (!float.TryParse(rule.Argument, out var sizeY))
                    {
                        continue;
                    }
                    
                    _CreateChildContext(new Vector3(0.0f, 0.0f, 0.0f), new Vector2(Size.x, sizeY), Quaternion.identity, rule);

                    RelativeSize.y -= sizeY;
                    RelativePosition.y = sizeY;
                }

                if (rule.Concat.Equals("YStaticSplit"))
                {
                    if (!float.TryParse(rule.Argument, out var sizeY))
                    {
                        continue;
                    }

                    _CreateChildContext(new Vector3(0.0f, Size.y - sizeY, 0.0f), new Vector2(Size.x, sizeY), Quaternion.identity, rule);

                    RelativeSize.y -= sizeY;
                }
            }

            float scalableMinX = 0.0f;
            float scalableMaxX = 0.0f;
            float scalableSizeX = 0.0f;

            float scalableMinY = 0.0f;
            float scalableMaxY = 0.0f;
            float scalableSizeY = 0.0f;

            foreach (var rule in rules)
            {
                if (rule.Concat.Equals("RepeatX"))
                {
                    if (!float.TryParse(rule.Argument, out var sizeX))
                    {
                        continue;
                    }

                    float countFloat = RelativeSize.x / sizeX;
                    int count = (int)countFloat;
                    scalableSizeX = (RelativeSize.x - (sizeX * count)) * 0.5f;
                    scalableMinX = RelativePosition.x;
                    scalableMaxX = sizeX * count + scalableMinX + scalableSizeX;
                    float remainX = scalableSizeX + RelativePosition.x;

                    while (remainX < RelativeSize.x + RelativePosition.x - sizeX * 0.5f)
                    {
                        _CreateChildContext(new Vector3(remainX, 0.0f, 0.0f), new Vector2(sizeX, Size.y), Quaternion.identity, rule);
                        remainX += sizeX;
                    }
                }
                
                if (rule.Concat.Equals("RepeatY"))
                {
                    if (!float.TryParse(rule.Argument, out var sizeY))
                    {
                        continue;
                    }

                    float countFloat = RelativeSize.y / sizeY;
                    int count = (int)countFloat;
                    scalableSizeY = (RelativeSize.y - (sizeY * count)) * 0.5f;
                    scalableMinY = RelativePosition.y;
                    scalableMaxY = sizeY * count + scalableMinY + scalableSizeY;
                    float remainY = scalableSizeY + RelativePosition.y;

                    while (remainY < RelativeSize.y + RelativePosition.y - sizeY * 0.5f)
                    {
                        _CreateChildContext(new Vector3(0.0f, remainY, 0.0f), new Vector2(Size.x, sizeY), Quaternion.identity, rule);
                        remainY += sizeY;
                    }
                }

                if (rule.Concat.Equals("Primitive"))
                {
                    IsPrimitive = true;
                    PrimitivePool = ObjectPoolContainer.Instance.GetPool(rule.Argument);
                }
            }

            foreach (var rule in rules)
            {
                if (rule.Concat.Equals("ScalableX"))
                {
                    if (!float.TryParse(rule.Argument, out var realY))
                    {
                        continue;
                    }
                    
                    var scalable = _CreateChildContext(new Vector3(scalableMinX, 0.0f, 0.0f), new Vector2(scalableSizeX, Size.y / realY), Quaternion.identity, rule);
                    scalable.UseLocalScale = true;
                }
                
                if (rule.Concat.Equals("XScalable"))
                {
                    if (!float.TryParse(rule.Argument, out var realY))
                    {
                        continue;
                    }
                    
                    var scalable = _CreateChildContext(new Vector3(scalableMaxX, 0.0f, 0.0f), new Vector2(scalableSizeX, Size.y / realY), Quaternion.identity, rule);
                    scalable.UseLocalScale = true;
                }

                if (rule.Concat.Equals("ScalableY"))
                {
                    if (!float.TryParse(rule.Argument, out var realX))
                    {
                        continue;
                    }

                    var scalable = _CreateChildContext(new Vector3(0.0f, scalableMinY, 0.0f), new Vector2(Size.x / realX, scalableSizeY), Quaternion.identity, rule);

                    if (rule.ChildRules.Count <= 1)
                    {
                        scalable.UseLocalScale = true;
                    }

                    
                }

                if (rule.Concat.Equals("YScalable"))
                {
                    if (!float.TryParse(rule.Argument, out var realX))
                    {
                        continue;
                    }

                    var scalable = _CreateChildContext(new Vector3(0.0f, scalableMaxY, 0.0f), new Vector2(Size.x / realX, scalableSizeY), Quaternion.identity, rule);
                    if (rule.ChildRules.Count <= 1)
                    {
                        scalable.UseLocalScale = true;
                    }

                }
            }
        }

        private Context _CreateChildContext(Vector3 pos, Vector3 size, Quaternion rot, Rule rule)
        {
            Context childContext = new Context();

            childContext.Position = pos;
            childContext.Size = size;
            childContext.RelativeSize = size;
            childContext.Rotation = rot;
                        
            childContext.ParseRule(rule.ChildRules);

            ChildContext.Add(childContext);

            return childContext;
        }

        public void CreatePrimitive(GameObject parentObject)
        {
            if (IsPrimitive)
            {
                _attachedObject = PrimitivePool.Get();    
            }
            else
            {
                if(_attachedObject == null)
                {
                    _attachedObject = new GameObject(Name);
                }
            }
                
            //contextObject = new GameObject(Name);
            _attachedObject.transform.SetParent(parentObject.transform);
            _attachedObject.transform.localPosition = Position;
            _attachedObject.transform.localRotation = Rotation;
            if (UseLocalScale)
            {
                _attachedObject.transform.localScale = new Vector3(Size.x, Size.y, 1.0f);
            }
            else
            {
                _attachedObject.transform.localScale = Vector3.one;
            }
            
            foreach (var context in ChildContext)
            {
                context.CreatePrimitive(_attachedObject);
            }
        }
    }

    [System.Serializable]
    public class Rule
    {
        public string Name;
        public string Concat;
        public string Argument;
        public bool IsPrimitive = false;

        public List<Rule> ChildRules = new List<Rule>();
        public ObjectPool PrimitivePool;
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
                    Vector3 angleCornerPosition = new Vector3(Mathf.Sin(currentAngle) * data.Radius, 0.0f, Mathf.Cos(currentAngle) * data.Radius) + Position;
                    corners.Add(angleCornerPosition);
                    currentAngle += Mathf.Deg2Rad * deltaAngle;
                }
            }
            
            CreateFacade(height, corners);
        }

        public void CreateFacade(float height, List<Vector3> corners)
        {
            if(corners.Count <= 1)
            {
                return;
            }

            else if(corners.Count == 2)
            {
                _childContexts.Add(CreateFacadeWithCorner(height, corners[0], corners[1]));
            }

            else
            {
                for (int i = 0; i < corners.Count; i++)
                {
                    Vector3 t1 = corners[i];
                    Vector3 t2 = Vector3.zero;

                    if(corners.Count <= i + 1)
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
    [SerializeField] public List<ObjectPoolContainer.PoolData> _poolDatas;

    private GameObject _rootObject;
    [SerializeField] public Rule _rootRule;
    [SerializeField] public Rule _cornerRule;
}
