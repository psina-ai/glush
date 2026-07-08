using UnityEngine;

namespace Glush.Player
{
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerCameraLook : MonoBehaviour
    {
        [SerializeField] private PlayerConfig _config;
        [SerializeField] private Transform _cameraPivot;
        
        private PlayerInput _input;
        private float _currentPitch = 0f;
        
        private void Awake()
        {
            _input = GetComponent<PlayerInput>();
            
            // Блокировка курсора в режиме игры
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            // Инициализация текущего наклона
            if (_cameraPivot != null)
            {
                _currentPitch = _cameraPivot.localEulerAngles.x;
                if (_currentPitch > 180f) _currentPitch -= 360f;
            }
        }
        
        private void OnDestroy()
        {
            // Восстановление курсора при уничтожении объекта
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        private void Update()
        {
            if (_config == null || _cameraPivot == null)
            {
                if (_config == null) Debug.LogError($"[PlayerCameraLook] PlayerConfig не назначен на {name}");
                if (_cameraPivot == null) Debug.LogError($"[PlayerCameraLook] CameraPivot не назначен на {name}");
                return;
            }
            
            Vector2 lookInput = _input.LookInput;
            
            // Применение чувствительности
            lookInput *= _config.MouseSensitivity;
            
            // Инверсия по вертикали
            if (_config.InvertMouseY)
            {
                lookInput.y = -lookInput.y;
            }
            
            // Горизонтальное вращение (Yaw) - вращаем весь игрока
            float yawDelta = lookInput.x;
            transform.Rotate(Vector3.up, yawDelta);
            
            // Вертикальное вращение (Pitch) - только камера
            float pitchDelta = lookInput.y;
            _currentPitch -= pitchDelta; // минус, потому что вращение вверх должно уменьшать угол
            _currentPitch = Mathf.Clamp(_currentPitch, _config.LookPitchMin, _config.LookPitchMax);
            
            _cameraPivot.localRotation = Quaternion.Euler(_currentPitch, 0f, 0f);
        }
    }
}