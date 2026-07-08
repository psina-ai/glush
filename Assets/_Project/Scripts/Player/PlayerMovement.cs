using UnityEngine;

namespace Glush.Player
{
    [RequireComponent(typeof(CharacterController), typeof(PlayerInput))]
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private PlayerConfig _config;
        
        private CharacterController _controller;
        private PlayerInput _input;
        
        private Vector3 _currentVelocity = Vector3.zero;
        private float _verticalVelocity = 0f;
        private Vector3 _smoothVelocity = Vector3.zero;
        
        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<PlayerInput>();
        }
        
        private void OnEnable()
        {
            _input.OnJumpPressed += OnJump;
        }
        
        private void OnDisable()
        {
            _input.OnJumpPressed -= OnJump;
        }
        
        private void Update()
        {
            if (_config == null)
            {
                Debug.LogError($"[PlayerMovement] PlayerConfig не назначен на {name}");
                return;
            }
            
            // Расчёт целевой горизонтальной скорости
            Vector2 moveInput = _input.MoveInput;
            float targetSpeed = _input.SprintHeld ? _config.SprintSpeed : _config.WalkSpeed;
            Vector3 targetDirection = transform.forward * moveInput.y + transform.right * moveInput.x;
            Vector3 targetVelocity = targetDirection * targetSpeed;
            
            // Инерция разгона/остановки
            float smoothTime = moveInput.magnitude > 0.01f ? _config.AccelerationTime : _config.DecelerationTime;
            _currentVelocity = Vector3.SmoothDamp(_currentVelocity, targetVelocity, ref _smoothVelocity, smoothTime);
            
            // Гравитация
            if (_controller.isGrounded && _verticalVelocity < 0f)
            {
                // Небольшая отрицательная скорость, чтобы персонаж "прилипал" к земле
                _verticalVelocity = -2f;
            }
            else
            {
                _verticalVelocity += _config.Gravity * Time.deltaTime;
            }
            
            // Сборка итогового вектора движения
            Vector3 movement = _currentVelocity + Vector3.up * _verticalVelocity;
            _controller.Move(movement * Time.deltaTime);
        }
        
        private void OnJump()
        {
            if (_controller.isGrounded && _config != null)
            {
                // Формула вертикальной скорости для достижения заданной высоты прыжка
                _verticalVelocity = Mathf.Sqrt(-2f * _config.Gravity * _config.JumpHeight);
            }
        }
    }
}