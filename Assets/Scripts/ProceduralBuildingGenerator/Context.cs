using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using Utility.ObjectPool;

namespace ProceduralBuildingGenerator
{
    
    
    public class Context
    {
        public string Name;

        public Vector2 RelativeSize;
        public Vector2 Size;
        public Quaternion Rotation;
        public Vector3 Position;

        private bool _isPrimitive;
        private bool _useLocalScale;
        private Vector3 _relativePosition;
        private ObjectPool _primitivePool;

        private readonly List<Context> _childContext = new();

        private GameObject _attachedObject;

        public void ParseRule(List<Rule> rules)
        {
            foreach (var rule in rules)
            {
                switch (rule.Concat)
                {
                    case "StaticSplitX":
                    {
                        if (!float.TryParse(rule.Argument, out var sizeX))
                        {
                            continue;
                        }

                        CreateChildContext(new Vector3(0.0f, 0.0f, 0.0f), new Vector2(sizeX, Size.y),
                            Quaternion.identity,
                            rule);

                        RelativeSize.x -= sizeX;
                        _relativePosition.x = sizeX;
                        break;
                    }
                    case "XStaticSplit":
                    {
                        if (!float.TryParse(rule.Argument, out var sizeX))
                        {
                            continue;
                        }

                        CreateChildContext(new Vector3(Size.x - sizeX, 0.0f, 0.0f), new Vector2(sizeX, Size.y),
                            Quaternion.identity, rule);

                        RelativeSize.x -= sizeX;
                        break;
                    }
                    case "StaticSplitY":
                    {
                        if (!float.TryParse(rule.Argument, out var sizeY))
                        {
                            continue;
                        }

                        CreateChildContext(new Vector3(0.0f, 0.0f, 0.0f), new Vector2(Size.x, sizeY),
                            Quaternion.identity,
                            rule);

                        RelativeSize.y -= sizeY;
                        _relativePosition.y = sizeY;
                        break;
                    }
                    case "YStaticSplit":
                    {
                        if (!float.TryParse(rule.Argument, out var sizeY))
                        {
                            continue;
                        }

                        CreateChildContext(new Vector3(0.0f, Size.y - sizeY, 0.0f), new Vector2(Size.x, sizeY),
                            Quaternion.identity, rule);

                        RelativeSize.y -= sizeY;
                        break;
                    }
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
                switch (rule.Concat)
                {
                    case "RepeatX":
                    {
                        if (!float.TryParse(rule.Argument, out var sizeX))
                        {
                            continue;
                        }

                        float countFloat = RelativeSize.x / sizeX;
                        int count = (int)countFloat;
                        scalableSizeX = (RelativeSize.x - (sizeX * count)) * 0.5f;
                        scalableMinX = _relativePosition.x;
                        scalableMaxX = sizeX * count + scalableMinX + scalableSizeX;
                        float remainX = scalableSizeX + _relativePosition.x;

                        while (remainX < RelativeSize.x + _relativePosition.x - sizeX * 0.5f)
                        {
                            CreateChildContext(new Vector3(remainX, 0.0f, 0.0f), new Vector2(sizeX, Size.y),
                                Quaternion.identity, rule);
                            remainX += sizeX;
                        }

                        break;
                    }
                    case "RepeatY":
                    {
                        if (!float.TryParse(rule.Argument, out var sizeY))
                        {
                            continue;
                        }

                        float countFloat = RelativeSize.y / sizeY;
                        int count = (int)countFloat;
                        scalableSizeY = (RelativeSize.y - (sizeY * count)) * 0.5f;
                        scalableMinY = _relativePosition.y;
                        scalableMaxY = sizeY * count + scalableMinY + scalableSizeY;
                        float remainY = scalableSizeY + _relativePosition.y;

                        while (remainY < RelativeSize.y + _relativePosition.y - sizeY * 0.5f)
                        {
                            CreateChildContext(new Vector3(0.0f, remainY, 0.0f), new Vector2(Size.x, sizeY),
                                Quaternion.identity, rule);
                            remainY += sizeY;
                        }

                        break;
                    }
                    case "Primitive":
                        _isPrimitive = true;
                        _primitivePool = ObjectPoolContainer.Instance.GetPool(rule.Argument);
                        break;
                }
            }

            foreach (var rule in rules)
            {
                switch (rule.Concat)
                {
                    case "ScalableX":
                    {
                        if (!float.TryParse(rule.Argument, out var realY))
                        {
                            continue;
                        }

                        var scalable = CreateChildContext(new Vector3(scalableMinX, 0.0f, 0.0f),
                            new Vector2(scalableSizeX, Size.y / realY), Quaternion.identity, rule);
                        scalable._useLocalScale = true;
                        break;
                    }
                    case "XScalable":
                    {
                        if (!float.TryParse(rule.Argument, out var realY))
                        {
                            continue;
                        }

                        var scalable = CreateChildContext(new Vector3(scalableMaxX, 0.0f, 0.0f),
                            new Vector2(scalableSizeX, Size.y / realY), Quaternion.identity, rule);
                        scalable._useLocalScale = true;
                        break;
                    }
                    case "ScalableY":
                    {
                        if (!float.TryParse(rule.Argument, out var realX))
                        {
                            continue;
                        }

                        var scalable = CreateChildContext(new Vector3(0.0f, scalableMinY, 0.0f),
                            new Vector2(Size.x / realX, scalableSizeY), Quaternion.identity, rule);

                        if (rule.ChildRules.Count <= 1)
                        {
                            scalable._useLocalScale = true;
                        }

                        break;
                    }
                    case "YScalable":
                    {
                        if (!float.TryParse(rule.Argument, out var realX))
                        {
                            continue;
                        }

                        var scalable = CreateChildContext(new Vector3(0.0f, scalableMaxY, 0.0f),
                            new Vector2(Size.x / realX, scalableSizeY), Quaternion.identity, rule);
                        if (rule.ChildRules.Count <= 1)
                        {
                            scalable._useLocalScale = true;
                        }

                        break;
                    }
                }
            }

            foreach (Rule rule in rules)
            {
                if (rule.Concat == "SubDivRandomX")
                {
                    string pattern = @"\[(\d+),\s*([^\]]+)\]";
                    
                    List<(float, Rule)> subDivs = new List<(float, Rule)>();
                    var matches = Regex.Matches(rule.Argument, pattern);
                    float childMinSize = float.MaxValue;

                    foreach (Match match in matches)
                    {
                        Rule find = rule.ChildRules.Find((arg) => match.Groups[2].Value.Equals(arg.Name));

                        if (find == null)
                        {
                            continue;
                        }
                        
                        float childContextSize = float.Parse(match.Groups[1].Value);

                        if (childContextSize < childMinSize)
                        {
                            childMinSize = childContextSize;
                        }
                        
                        subDivs.Add((childContextSize, find));
                    }

                    if (childMinSize > Size.x)
                    {
                        break;
                    }

                    float currentXPosition = 0.0f;
                    List<Rule> removeRules = new List<Rule>();

                    while (currentXPosition < Size.x)
                    {
                        foreach (var removeCheckSubDiv in subDivs)
                        {
                            if (removeCheckSubDiv.Item1 > Size.x - currentXPosition)
                            {
                                removeRules.Add(removeCheckSubDiv.Item2);
                            }
                        }

                        foreach (Rule removeRule in removeRules)
                        {
                            subDivs.Remove(subDivs.Find((s) => s.Item2.Equals(removeRule)));
                        }

                        removeRules.Clear();

                        if (subDivs.Count == 0)
                        {
                            break;
                        }
                        
                        var subDiv = subDivs[Random.Range(0, subDivs.Count)];
                        
                        var childContext = CreateChildContext(new Vector3(scalableSizeX + currentXPosition, 0.0f, 0.0f), Size,
                            Quaternion.identity, subDiv.Item2);
                        childContext._useLocalScale = false;
                        currentXPosition += subDiv.Item1;
                    }
                }
            }
        }

        private Context CreateChildContext(Vector3 pos, Vector3 size, Quaternion rot, Rule rule)
        {
            Context childContext = new()
            {
                Position = pos,
                Size = size,
                RelativeSize = size,
                Rotation = rot
            };

            childContext.ParseRule(rule.ChildRules);

            _childContext.Add(childContext);

            return childContext;
        }

        public void CreatePrimitive(GameObject parentObject)
        {
            if (_isPrimitive)
            {
                _attachedObject = _primitivePool.Get();
            }
            else
            {
                if (_attachedObject == null)
                {
                    _attachedObject = new GameObject(Name);
                }
            }

            _attachedObject.transform.SetParent(parentObject.transform);
            _attachedObject.transform.localPosition = Position;
            _attachedObject.transform.localRotation = Rotation;
            _attachedObject.transform.localScale = _useLocalScale ? new Vector3(Size.x, Size.y, 1.0f) : Vector3.one;

            foreach (var context in _childContext)
            {
                context.CreatePrimitive(_attachedObject);
            }
        }
    }
}
