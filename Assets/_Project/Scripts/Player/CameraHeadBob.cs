using UnityEngine;

namespace Glush.Player
{
    [RequireComponent(typeof(PlayerInput))]
    public class CameraHeadBob : MonoBehaviour
    {
        [SerializeField] private PlayerConfig _config;
        [SerializeField] private Transform _cameraPivot;
        [SerializeField] private CharacterController _controller;
        
        private PlayerInput _input;
        private Vector3 _originalPivotLocalPos;
        private float _bobTimer = 0f;
        private float _bobIntensity = 0f;
        
        private void Awake()
        {
            _input = GetComponent<PlayerInput>();
            
            if (_controller == null)
            {
                _controller = GetComponentInParent<CharacterController>();
                if (_controller == null)
                {
                    Debug.LogError($"[CameraHeadBob] CharacterController не найден на {name} или его родителях");
                }
            }
        }
        
        private void Start()
        {
            if (_cameraPivot != null)
            {
                _originalPivotLocalPos = _cameraPivot.localPosition;
            }
            else
            {
                Debug.LogError($"[CameraHeadBob] CameraPivot не назначен на {name}");
            }
        }
        
        private void Update()
        {
            if (_config == null || _cameraPivot == null || _controller == null)
            {
                return;
            }
            
            bool shouldBob = ShouldApplyBob();
            float targetIntensity = shouldBob ? 1f : 0f;
            
            // Плавное изменение интенсивности
            _bobIntensity = Mathf.Lerp(_bobIntensity, targetIntensity, _config.BobLerpSpeed * Time.deltaTime);
            
            if (_bobIntensity < 0.001f)
            {
                // Если интенсивность почти нулевая, возвращаем камеру в исходное положение
                _cameraPivot.localPosition = _originalPivotLocalPos;
                _bobTimer = 0f;
                return;
            }
            
            // Параметры bob в зависимости от бега/ходьбы
            float amplitude = _input.SprintHeld ? _config.SprintBobAmplitude : _config.WalkBobAmplitude;
            float frequency = _input.SprintHeld ? _config.SprintBobFrequency : _config.WalkBobFrequency;
            
            // Увеличиваем таймер с учётом скорости
            float horizontalSpeed = new Vector3(_controller.velocity.x, 0f, _controller.velocity.z).magnitude;
            if (horizontalSpeed > 0.1f)
            {
                _bobTimer += Time.deltaTime * frequency * Mathf.PI * 2f;
            }
            
            // Вертикальное покачивание (синус)
            float verticalBob = Mathf.Sin(_bobTimer) * amplitude * _bobIntensity;
            
            // Горизонтальное покачивание (косинус) - в 2 раза меньшая амплитуда и частота
            float horizontalBob = Mathf.Cos(_bobTimer * 0.5f) * amplitude * 0.5f * _bobIntensity;
            
            // Применяем смещение
            _cameraPivot.localPosition = _originalPivotLocalPos + new Vector3(horizontalBob, verticalBob, 0f);
        }
        
        private bool ShouldApplyBob()
        {
            if (!_config.EnableHeadBob)
            {
                return false;
            }
            
            if (!_controller.isGrounded)
            {
                return false;
            }
            
            // Проверяем, движется ли игрок по горизонтали
            Vector3 horizontalVelocity = new Vector3(_controller.velocity.x, 0f, _controller.velocity.z);
            float speed = horizontalVelocity.magnitude;
            
            // Минимальная скорость для включения bob
            return speed > 0.1f;
        }
        
        // Публичный метод для получения фазы bob (используется PlayerFootsteps)
        public float GetBobPhase()
        {
            // Возвращаем нормализованное значение синуса от -1 до 1
            if (_bobIntensity < 0.01f)
            {
                return 0f;
            }
            return Mathf.Sin(_bobTimer);
        }
    }
}