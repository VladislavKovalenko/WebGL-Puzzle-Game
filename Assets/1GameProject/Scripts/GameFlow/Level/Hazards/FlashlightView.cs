using UnityEngine;
using UnityEngine.UI;

namespace _1GameProject.Scripts.GameFlow.Level.Hazards
{
    [RequireComponent(typeof(Image))]
    public class FlashlightView : MonoBehaviour
    {
        [SerializeField] private Material _fogMaterial;

        [Header("Follow Settings")]
        [Tooltip("Скорость догоняния (больше = быстрее)")]
        [SerializeField] private float _followSpeed = 8f;

        [Tooltip("Максимальная дистанция отставания в пикселях. " +
                 "Если курсор уйдёт дальше — луч телепортируется ближе.")]
        [SerializeField] private float _maxLagDistance = 300f;

        private Image _image;
        private Material _materialInstance;

        // Текущая сглаженная позиция фонарика
        private Vector2 _currentPosition;
        // Целевая позиция (куда двигается курсор)
        private Vector2 _targetPosition;
        // Инициализирована ли начальная позиция
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

        /// <summary>
        /// Вызывается каждый кадр из HazardPresenter.
        /// screenPosition — Input.mousePosition или touch position.
        /// </summary>
        public void UpdatePosition(Vector2 screenPosition)
        {
            if (_materialInstance == null) return;

            _targetPosition = screenPosition;

            // Первый кадр — телепортируемся к курсору без задержки
            if (!_positionInitialized)
            {
                _currentPosition = _targetPosition;
                _positionInitialized = true;
            }

            // Плавное следование (экспоненциальное сглаживание)
            _currentPosition = Vector2.Lerp(
                _currentPosition,
                _targetPosition,
                1f - Mathf.Exp(-_followSpeed * Time.deltaTime)
            );

            // Ограничение максимального отставания
            Vector2 diff = _targetPosition - _currentPosition;
            if (diff.magnitude > _maxLagDistance)
            {
                _currentPosition = _targetPosition - diff.normalized * _maxLagDistance;
            }

            _materialInstance.SetVector(FlashlightCenterID,
                new Vector4(_currentPosition.x, _currentPosition.y, 0, 0));
        }

        // Настройка из кода
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

        public void SetFollowSpeed(float speed)
        {
            _followSpeed = speed;
        }

        public void SetMaxLagDistance(float distance)
        {
            _maxLagDistance = distance;
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