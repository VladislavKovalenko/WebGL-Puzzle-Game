using UnityEngine;
using UnityEngine.UI;

namespace _1GameProject.Scripts.GameFlow.Level.Hazards
{
    [RequireComponent(typeof(Image))]
    public class FlashlightView : MonoBehaviour
    {
        [SerializeField] private Material _fogMaterial;

        [Header("Follow Settings (Для ПК)")]
        [SerializeField] private float _followSpeed = 8f;
        [SerializeField] private float _maxLagDistance = 300f;

        [Header("Auto-Move Settings (Для Мобилок)")]
        [Tooltip("Ограничение движения по X (0 = левый край, 1 = правый)")]
        [SerializeField, Range(0f, 1f)] private float _minXPercent = 0.2f;
        [SerializeField, Range(0f, 1f)] private float _maxXPercent = 0.8f;

        [Tooltip("Ограничение движения по Y (0 = низ экрана, 1 = верх)")]
        [SerializeField, Range(0f, 1f)] private float _minYPercent = 0.3f;
        [SerializeField, Range(0f, 1f)] private float _maxYPercent = 0.7f;

        public float MinXPercent => _minXPercent;
        public float MaxXPercent => _maxXPercent;
        public float MinYPercent => _minYPercent;
        public float MaxYPercent => _maxYPercent;

        private Image _image;
        private Material _materialInstance;

        private Vector2 _currentPosition;
        private Vector2 _targetPosition;
        private bool _positionInitialized;

        private static readonly int FlashlightCenterID = Shader.PropertyToID("_FlashlightCenter");
        private static readonly int RadiusID           = Shader.PropertyToID("_Radius");
        private static readonly int SoftnessID         = Shader.PropertyToID("_Softness");

        public void Init()
        {
            gameObject.SetActive(true);
            _image = GetComponent<Image>();

            if (_fogMaterial != null)
            {
                _materialInstance = new Material(_fogMaterial);
                _image.material = _materialInstance;
            }

            _positionInitialized = false;
        }

        public void Disable()
        {
            gameObject.SetActive(false);
        }

        public void UpdatePosition(Vector2 screenPosition)
        {
            if (_materialInstance == null) return;

            _targetPosition = screenPosition;

            if (!_positionInitialized)
            {
                _currentPosition = _targetPosition;
                _positionInitialized = true;
            }

            _currentPosition = Vector2.Lerp(
                _currentPosition,
                _targetPosition,
                1f - Mathf.Exp(-_followSpeed * Time.deltaTime)
            );

            Vector2 diff = _targetPosition - _currentPosition;
            if (diff.magnitude > _maxLagDistance)
            {
                _currentPosition = _targetPosition - diff.normalized * _maxLagDistance;
            }

            _materialInstance.SetVector(FlashlightCenterID,
                new Vector4(_currentPosition.x, _currentPosition.y, 0, 0));
        }

        public void SetRadius(float pixels)
        {
            if (_materialInstance != null)
                _materialInstance.SetFloat(RadiusID, pixels);
        }

        public void SetSoftness(float pixels)
        {
            if (_materialInstance != null)
                _materialInstance.SetFloat(SoftnessID, pixels);
        }

        private void OnDestroy()
        {
            if (_materialInstance != null)
            {
                Destroy(_materialInstance);
            }
        }
    }
}
