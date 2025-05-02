using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LSystemTester : MonoBehaviour
{
    [SerializeField] private int _seed;
    [SerializeField] private GameObject _boxPrefab;
    [SerializeField] private ProceduralBuildingGenerator _buildingGenerator;

    [SerializeField] private float _roadLength;
    [SerializeField] private int _maxCreateCount = 10;

    [SerializeField] private GameObject[] _roadPrefabs;
    [SerializeField] private Material[] _crossPointMaterials;
    
    [SerializeField] private Transform _startPositionParent;

    [SerializeField] private GameObject _crossPointPrefab;

    private LSystem _lSystem;

    private List<Context> _contexts = new List<Context>();
    private List<ContextRule> _rules = new List<ContextRule>();
    private List<Edge> _edges = new List<Edge>();

    private Dictionary<Vector3, int> _crossPoint = new Dictionary<Vector3, int>();
    private List<Vector3> _crossPointList = new List<Vector3>();

    private int _currentCreateCount = 0;

    class Context
    {
        public string Name;
        public int Priority;
        public GameObject Object;
        public Vector3 StartPos;
        public Vector3 EndPos;
        public float Rotation;
        public Edge Edge => new Edge() { P1 = StartPos, P2 = EndPos };
    }

    class ContextRule
    {
        public string Previous;
        public float weight;
        public List<NextData> Next = new List<NextData>();

        public class NextData
        {
            public string Name;
            public GameObject Prefab;
            public int Priority;
            public float Length;
            public float Rotation;    
        }
    }

    class IntersectData
    {
        public bool IsIntersect = false;
        public float ToPointRotation;
        public Edge edge;
        public Vector3 Point;
    }

    class ContextRuleCollection
    {
        public List<ContextRule> Rules = new List<ContextRule>();
        public float TotalWeight;

        public ContextRule GetRandomRule()
        {
            float random = Random.Range(0.0f, TotalWeight);
            float tempRandomValue = 0.0f;

            foreach (var rule in Rules)
            {
                if (tempRandomValue <= random && random < rule.weight)
                {
                    return rule;
                }

                tempRandomValue += rule.weight;
            }

            return Rules[^1];
        }
    }

    private Dictionary<string, ContextRuleCollection> _ruleCollections = new Dictionary<string, ContextRuleCollection>();
    // Start is called before the first frame update
    void Start()
    {
        var rule1 = new ContextRule
        {
            Previous = "MainRoad",
            weight = 0.4f
        };
        rule1.Next.Add(new ContextRule.NextData()
        {
            Name = "MainRoad",
            Prefab = _roadPrefabs[0],
            Priority = 1,
            Length = 20.0f,
            Rotation = 0.0f,
        });
        
        AddRule(rule1);
        
        var rule2 = new ContextRule
        {
            weight = 0.4f,
            Previous = "MainRoad"
        };
        rule2.Next.Add(new ContextRule.NextData()
        {
            Name = "MainRoad",
            Priority = 1,
            Prefab = _roadPrefabs[0],
            Length = 20.0f,
            Rotation = 0.0f,
        });
        
        rule2.Next.Add(new ContextRule.NextData()
        {
            Name = "SubRoad",
            Prefab = _roadPrefabs[1],
            Priority = 2,
            Length = 20.0f,
            Rotation = 90.0f,
        });
        
        AddRule(rule2);
        
        var rule3 = new ContextRule
        {
            Previous = "SubRoad",
            weight = 0.2f
        };
        rule3.Next.Add(new ContextRule.NextData()
        {
            Name = "SubRoad",
            Prefab = _roadPrefabs[1],
            Priority = 2,
            Length = 20.0f,
            Rotation = 0.0f,
        });
        rule3.Next.Add(new ContextRule.NextData()
        {
            Name = "SubRoad",
            Prefab = _roadPrefabs[1],
            Priority = 2,
            Length = 20.0f,
            Rotation = 90.0f,
        });
        
        AddRule(rule3);
        
        var rule4 = new ContextRule
        {
            Previous = "SubRoad",
            weight = 0.2f
        };
        rule4.Next.Add(new ContextRule.NextData()
        {
            Name = "SubRoad",
            Prefab = _roadPrefabs[1],
            Length = 20.0f,
            Rotation = 0.0f,
        });
        
        AddRule(rule4);

        for (int i = 0; i < _startPositionParent.childCount; i++)
        {
            var t1 = _startPositionParent.GetChild(i);
            var t2 = _startPositionParent.GetChild(i + 1 >= _startPositionParent.childCount ? 0 : i + 1);
            
            t1.transform.LookAt(t2);
            t1.transform.rotation *= Quaternion.Euler(0.0f, -90.0f, 0.0f);
            
            CreateContext(_contexts, null, rule1, t1.position, t1.rotation.eulerAngles.y);
        }

        StartCoroutine(CreateRoot());
    }

    IEnumerator CreateRoot()
    {
        while(_contexts.Count != 0)
        {
            _contexts.Sort((c1, c2) => c1.Priority.CompareTo(c2.Priority));
            var context = _contexts[0];
            
            foreach (var ruleCollection in _ruleCollections)
            {
                if (context.Name.Equals(ruleCollection.Key))
                {
                    var contextRule = ruleCollection.Value.GetRandomRule();
                    CreateContext(_contexts, context, contextRule, context.EndPos, context.Rotation);
                    yield return new WaitForSeconds(0.03f);
                }

                if (_currentCreateCount >= _maxCreateCount)
                {
                    yield break;
                }
            }
            
            _contexts.RemoveAt(0);
        }
    }

    void AddRule(ContextRule rule)
    {
        if (!_ruleCollections.TryGetValue(rule.Previous, out var ruleCollection))
        {
            ruleCollection = new ContextRuleCollection();
            
            _ruleCollections.Add(rule.Previous, ruleCollection);
        }
        
        ruleCollection.Rules.Add(rule);
        ruleCollection.TotalWeight += rule.weight;
    }

    void CreateContext(List<Context> contexts, Context previousContext, ContextRule rule, Vector3 startPos, float startRotation)
    {
        var addedEdges = new List<Edge>();
        
        foreach (var nextData in rule.Next)
        {
            float rotation = startRotation + nextData.Rotation;
            Quaternion rot = Quaternion.Euler(0.0f, rotation, 0.0f);
            Vector3 direction = rot * Vector3.right; 
            Vector3 endPos = (direction * nextData.Length) + startPos;
            
            var intersectEdge = GetIntersectEdge(startPos, endPos, previousContext?.Edge);
            Vector3 finalEndPos = intersectEdge.Point;

            AddCrossPoint(finalEndPos);
            
            var context = new Context
            {
                Name = (intersectEdge.edge  != null || intersectEdge.IsIntersect) ? "Intersect" : nextData.Name,
                Priority = nextData.Priority,
                Rotation = intersectEdge.ToPointRotation,
                StartPos = startPos,
                EndPos = finalEndPos
            };
            
            var box = Instantiate(nextData.Prefab);
            box.name = context.Name;
            box.transform.position = startPos;
            box.transform.rotation = Quaternion.Euler(0.0f, intersectEdge.ToPointRotation, 0.0f);
            box.transform.localScale =
                new Vector3(Vector3.Distance(startPos, finalEndPos), 1.0f, 2.0f);
            
            addedEdges.Add(context.Edge);

            if (!context.Name.Equals("Intersect"))
            {
                contexts.Add(context);
            }

            _currentCreateCount++;
        }
        
        _edges.AddRange(addedEdges);
    }
    
    void AddCrossPoint(Vector3 pos)
    {
        _crossPoint.TryAdd(pos, 0);
        _crossPoint[pos]++;
    }

    IntersectData GetIntersectEdge(Vector3 startPos, Vector3 endPos, Edge exception)
    {
        var intersectData = new IntersectData();
        List<Vector3> crossPoints = new List<Vector3>();
        List<Vector3> intersectPoints = new List<Vector3>();
        
        foreach (var edge in _edges)
        {
            if (exception?.Equals(edge) ?? false)
            {
                continue;
            }

            
            if (MathUtil.IsCrossLine(startPos, endPos, edge.P1, edge.P2))
            {
                intersectData.IsIntersect = true;
                var crossPoint = MathUtil.GetCrossPoint(startPos, endPos, edge.P1, edge.P2);
                crossPoints.Add(crossPoint);
            }

            else
            {
                var endPointIntersectData = MathUtil.GetCircleLineIntersect(edge.P1, edge.P2, endPos, 5.0f);

                if (endPointIntersectData.IsIntersect)
                {
                    intersectData.IsIntersect = true;
                    crossPoints.Add(endPointIntersectData.LinePoint);
                }
            }
        }

        // foreach (var crossPoint in _crossPoint)
        // {
        //     if (MathUtil.GetCircleLineIntersect(startPos, endPos, crossPoint.Key, 5.0f).IsIntersect)
        //     {
        //         crossPoints.Add(crossPoint.Key);
        //         intersectData.IsIntersect = true;
        //         intersectPoints.Add(crossPoint.Key);
        //     }
        // }

        if (!intersectData.IsIntersect)
        {
            intersectData.Point = endPos;
        }
        else
        {
            float minSqrMag = float.PositiveInfinity;
            foreach (var crossPoint in crossPoints)
            {
                float mag = Vector3.SqrMagnitude(crossPoint - startPos);
                if (mag <= minSqrMag)
                {
                    intersectData.Point = crossPoint;
                    minSqrMag = mag;
                }
            }
        }
        
        intersectData.ToPointRotation = MathUtil.GetDirRotation(startPos, intersectData.Point);

        return intersectData;
    }

    
}
