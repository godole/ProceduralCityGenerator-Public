using System.Collections.Generic;
using UnityEngine;

namespace ProceduralBuildingGenerator
{
    [System.Serializable]
    public class Rule
    {
        public string Name;
        public string Concat;
        public string Argument;
        public List<Rule> ChildRules = new List<Rule>();
    }
    
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

        public List<Context> ChildContext = new();

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

                    CreateChildContext(new Vector3(0.0f, 0.0f, 0.0f), new Vector2(sizeX, Size.y),
                        Quaternion.identity,
                        rule);

                    RelativeSize.x -= sizeX;
                    RelativePosition.x = sizeX;
                }

                if (rule.Concat.Equals("XStaticSplit"))
                {
                    if (!float.TryParse(rule.Argument, out var sizeX))
                    {
                        continue;
                    }

                    CreateChildContext(new Vector3(Size.x - sizeX, 0.0f, 0.0f), new Vector2(sizeX, Size.y),
                        Quaternion.identity, rule);

                    RelativeSize.x -= sizeX;
                }

                if (rule.Concat.Equals("StaticSplitY"))
                {
                    if (!float.TryParse(rule.Argument, out var sizeY))
                    {
                        continue;
                    }

                    CreateChildContext(new Vector3(0.0f, 0.0f, 0.0f), new Vector2(Size.x, sizeY),
                        Quaternion.identity,
                        rule);

                    RelativeSize.y -= sizeY;
                    RelativePosition.y = sizeY;
                }

                if (rule.Concat.Equals("YStaticSplit"))
                {
                    if (!float.TryParse(rule.Argument, out var sizeY))
                    {
                        continue;
                    }

                    CreateChildContext(new Vector3(0.0f, Size.y - sizeY, 0.0f), new Vector2(Size.x, sizeY),
                        Quaternion.identity, rule);

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
                        CreateChildContext(new Vector3(remainX, 0.0f, 0.0f), new Vector2(sizeX, Size.y),
                            Quaternion.identity, rule);
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
                        CreateChildContext(new Vector3(0.0f, remainY, 0.0f), new Vector2(Size.x, sizeY),
                            Quaternion.identity, rule);
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

                    var scalable = CreateChildContext(new Vector3(scalableMinX, 0.0f, 0.0f),
                        new Vector2(scalableSizeX, Size.y / realY), Quaternion.identity, rule);
                    scalable.UseLocalScale = true;
                }

                if (rule.Concat.Equals("XScalable"))
                {
                    if (!float.TryParse(rule.Argument, out var realY))
                    {
                        continue;
                    }

                    var scalable = CreateChildContext(new Vector3(scalableMaxX, 0.0f, 0.0f),
                        new Vector2(scalableSizeX, Size.y / realY), Quaternion.identity, rule);
                    scalable.UseLocalScale = true;
                }

                if (rule.Concat.Equals("ScalableY"))
                {
                    if (!float.TryParse(rule.Argument, out var realX))
                    {
                        continue;
                    }

                    var scalable = CreateChildContext(new Vector3(0.0f, scalableMinY, 0.0f),
                        new Vector2(Size.x / realX, scalableSizeY), Quaternion.identity, rule);

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

                    var scalable = CreateChildContext(new Vector3(0.0f, scalableMaxY, 0.0f),
                        new Vector2(Size.x / realX, scalableSizeY), Quaternion.identity, rule);
                    if (rule.ChildRules.Count <= 1)
                    {
                        scalable.UseLocalScale = true;
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
                if (_attachedObject == null)
                {
                    _attachedObject = new GameObject(Name);
                }
            }

            _attachedObject.transform.SetParent(parentObject.transform);
            _attachedObject.transform.localPosition = Position;
            _attachedObject.transform.localRotation = Rotation;
            _attachedObject.transform.localScale = UseLocalScale ? new Vector3(Size.x, Size.y, 1.0f) : Vector3.one;

            foreach (var context in ChildContext)
            {
                context.CreatePrimitive(_attachedObject);
            }
        }
    }
}
