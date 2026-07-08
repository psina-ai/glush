using UnityEngine;
using System;

namespace Glush.Player
{
    public enum SurfaceType
    {
        Unknown,
        Concrete,
        Wood,
        Grass,
        Sand,
        Metal,
        Water
    }
    
    [RequireComponent(typeof(CharacterController))]
    public class PlayerFootsteps : MonoBehaviour
    {
        [SerializeField] private CameraHeadBob _headBob;
        [SerializeField] private LayerMask _groundMask = ~0;
        [SerializeField] private float _raycastDistance = 1.5f;
        [SerializeField] private bool _logFootsteps = false;
        
        private CharacterController _controller;
        private float _lastFootstepPhase = 0f;
        private bool _wasMoving = false;
        
        public event Action<SurfaceType, Vector3> OnFootstep;
        
        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            
            if (_headBob == null)
            {
                _headBob = GetComponent<CameraHeadBob>();
                if (_headBob == null)
                {
                    Debug.LogWarning($"[PlayerFootsteps] CameraHeadBob не найден на {name}");
                }
            }
        }
        
        private void Update()
        {
            if (_controller == null)
            {
                return;
            }
            
            // Проверяем, движется ли игрок и на земле ли он
            bool isMoving = IsPlayerMoving();
            bool isGrounded = _controller.isGrounded;
            
            if (!isMoving || !isGrounded)
            {
                _wasMoving = false;
                return;
            }
            
            // Если есть head bob, синхронизируемся с его фазой
            if (_headBob != null)
            {
                float currentPhase = _headBob.GetBobPhase();
                
                // Определяем момент "нижней точки" - когда фаза переходит от отрицательной к положительной
                // (синус проходит минимум)
                if (_lastFootstepPhase < 0f && currentPhase >= 0f)
                {
                    TriggerFootstep();
                }
                
                _lastFootstepPhase = currentPhase;
            }
            else
            {
                // Fallback: шаги по времени (менее точные)
                if (!_wasMoving)
                {
                    TriggerFootstep();
                }
            }
            
            _wasMoving = true;
        }
        
        private bool IsPlayerMoving()
        {
            if (_controller == null)
            {
                return false;
            }
            
            Vector3 horizontalVelocity = new Vector3(_controller.velocity.x, 0f, _controller.velocity.z);
            return horizontalVelocity.magnitude > 0.1f;
        }
        
        private void TriggerFootstep()
        {
            // Определяем тип поверхности под ногами
            SurfaceType surface = DetectSurfaceType();
            Vector3 footPosition = transform.position + Vector3.down * (_controller.height * 0.5f);
            
            // Вызываем событие
            OnFootstep?.Invoke(surface, footPosition);
            
            // Логирование
            if (_logFootsteps)
            {
                Debug.Log($"[Footstep] Surface: {surface}");
            }
        }
        
        private SurfaceType DetectSurfaceType()
        {
            // Raycast вниз для определения поверхности
            Vector3 rayOrigin = transform.position;
            RaycastHit hit;
            
            if (Physics.Raycast(rayOrigin, Vector3.down, out hit, _raycastDistance, _groundMask))
            {
                // Проверяем тег объекта
                string tag = hit.collider.tag;
                
                switch (tag)
                {
                    case "Concrete": return SurfaceType.Concrete;
                    case "Wood": return SurfaceType.Wood;
                    case "Grass": return SurfaceType.Grass;
                    case "Sand": return SurfaceType.Sand;
                    case "Metal": return SurfaceType.Metal;
                    case "Water": return SurfaceType.Water;
                    default: return SurfaceType.Unknown;
                }
            }
            
            return SurfaceType.Unknown;
        }
        
        // Вспомогательный метод для отладки
        private void OnDrawGizmosSelected()
        {
            if (_controller != null)
            {
                Vector3 rayOrigin = transform.position;
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(rayOrigin, rayOrigin + Vector3.down * _raycastDistance);
            }
        }
    }
}