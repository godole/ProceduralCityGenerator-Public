using System;
using CityGenerator.TensorFields;
using UnityEngine;
using UnityEngine.Serialization;

namespace CityGenerator
{
    public class LineTensorFieldGenerator : MonoBehaviour
    {
        [SerializeField] private LineTensorField _lineTensorField;
        [SerializeField] private Vector2 _size;
        
        private Camera _mainCamera;
        private Vector3 _prevMousePos;
        private bool _isDragging;

        private void Start()
        {
            _mainCamera = Camera.main;
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                _isDragging = true;
                _prevMousePos = _mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, _mainCamera.transform.position.y));
            }

            if (Input.GetMouseButtonUp(0))
            {
                _isDragging = false;
            }

            if (!_isDragging)
            {
                return;
            }
            
            Vector3 pos = _mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, _mainCamera.transform.position.y));

            if (pos.x < 0.0f || pos.x > _size.x || pos.z < 0.0f || pos.z > _size.y)
            {
                return;
            }

            if (Vector3.Distance(pos, _prevMousePos) < 1.0f)
            {
                return;
            }
            
            var primitive = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            primitive.transform.position = pos;
            _lineTensorField.Positions.Add(pos);
            _prevMousePos = pos;
        }
    }
}
