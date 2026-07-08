using UnityEngine;

namespace Glush.Interaction
{
    [CreateAssetMenu(fileName = "InteractionConfig", menuName = "Glush/Interaction Config")]
    public class InteractionConfig : ScriptableObject
    {
        [Header("Raycast Settings")]
        [SerializeField, Tooltip("Дальность луча взаимодействия в метрах")]
        private float _raycastDistance = 1f;
        
        [SerializeField, Tooltip("Радиус SphereCast для более удобного выделения тонких объектов")]
        private float _raycastRadius = 0.15f;
        
        [SerializeField, Tooltip("Слой интерактивных объектов")]
        private LayerMask _interactableLayer = ~0;
        
        public float RaycastDistance => _raycastDistance;
        public float RaycastRadius => _raycastRadius;
        public LayerMask InteractableLayer => _interactableLayer;
        
        private void OnValidate()
        {
            _raycastDistance = Mathf.Max(0.1f, _raycastDistance);
            _raycastRadius = Mathf.Max(0f, _raycastRadius);
        }
    }
}