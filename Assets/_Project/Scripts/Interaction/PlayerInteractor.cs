using UnityEngine;
using System;
using Glush.Player;

namespace Glush.Interaction
{
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private InteractionConfig _config;
        [SerializeField] private Camera _camera;
        
        [Header("Debug Override")]
        [SerializeField, Tooltip("Переопределить дальность взаимодействия (только для отладки)")]
        private bool _overrideDistance = false;
        [SerializeField, Tooltip("Значение переопределения (если Override Distance включен)")]
        private float _overrideRaycastDistance = 1.25f;
        
        private PlayerInput _playerInput;
        private Interactable _currentFocus;
        private RaycastHit _hitInfo;
        
        public Interactable CurrentFocus => _currentFocus;
        public event Action<Interactable> OnFocusChanged;
        
        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
            
            if (_camera == null)
            {
                _camera = Camera.main;
                if (_camera == null)
                {
                    Debug.LogError($"[PlayerInteractor] Camera не назначена и Camera.main не найдена на {name}");
                }
            }
            
            if (_config == null)
            {
                Debug.LogError($"[PlayerInteractor] InteractionConfig не назначен на {name}");
            }
        }
        
        private void OnEnable()
        {
            _playerInput.OnInteractPressed += HandleInteractPressed;
        }
        
        private void OnDisable()
        {
            _playerInput.OnInteractPressed -= HandleInteractPressed;
            ClearFocus();
        }
        
        private void Update()
        {
            UpdateFocus();
        }
        
        private void UpdateFocus()
        {
            if (_config == null || _camera == null) return;
            
            Interactable newFocus = null;
            
            // SphereCast из центра камеры
            Vector3 rayOrigin = _camera.transform.position;
            Vector3 rayDirection = _camera.transform.forward;
            
            float raycastDistance = _overrideDistance ? _overrideRaycastDistance : _config.RaycastDistance;
            
            bool hasHit = Physics.SphereCast(
                rayOrigin, 
                _config.RaycastRadius, 
                rayDirection, 
                out _hitInfo, 
                raycastDistance, 
                _config.InteractableLayer
            );
            
            if (hasHit)
            {
                // Ищем компонент Interactable на самом объекте или его родителях
                newFocus = _hitInfo.collider.GetComponentInParent<Interactable>();
                
                if (newFocus != null && !newFocus.CanInteract(gameObject))
                {
                    newFocus = null;
                }
            }
            
            // Если фокус изменился
            if (newFocus != _currentFocus)
            {
                Interactable oldFocus = _currentFocus;
                _currentFocus = newFocus;
                
                // Уведомляем старый объект о потере фокуса
                if (oldFocus != null)
                {
                    oldFocus.NotifyUnfocused();
                }
                
                // Уведомляем новый объект о получении фокуса
                if (newFocus != null)
                {
                    newFocus.NotifyFocused();
                }
                
                OnFocusChanged?.Invoke(newFocus);
            }
        }
        
        private void HandleInteractPressed()
        {
            if (_currentFocus != null)
            {
                _currentFocus.Interact(gameObject);
            }
        }
        
        private void ClearFocus()
        {
            if (_currentFocus != null)
            {
                _currentFocus.NotifyUnfocused();
                Interactable oldFocus = _currentFocus;
                _currentFocus = null;
                OnFocusChanged?.Invoke(null);
            }
        }
        
        // Метод для отладки в редакторе
        private void OnDrawGizmosSelected()
        {
            if (_config == null || _camera == null) return;
            
            Gizmos.color = Color.green;
            Vector3 rayOrigin = _camera.transform.position;
            Vector3 rayDirection = _camera.transform.forward;
            float raycastDistance = _overrideDistance ? _overrideRaycastDistance : _config.RaycastDistance;
            Vector3 rayEnd = rayOrigin + rayDirection * raycastDistance;
            
            // Рисуем луч
            Gizmos.DrawLine(rayOrigin, rayEnd);
            
            // Рисуем сферу в конце луча для наглядности радиуса
            Gizmos.DrawWireSphere(rayEnd, _config.RaycastRadius);
            
            // Рисуем стартовую сферу
            Gizmos.DrawWireSphere(rayOrigin, _config.RaycastRadius);
        }
    }
}