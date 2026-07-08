using UnityEngine;

namespace Glush.Player
{
    [CreateAssetMenu(fileName = "PlayerConfig", menuName = "Glush/Player Config")]
    public class PlayerConfig : ScriptableObject
    {
        [Header("Movement")]
        [SerializeField, Tooltip("Скорость обычной ходьбы (м/с)")]
        private float _walkSpeed = 2.0f;
        
        [SerializeField, Tooltip("Скорость при удержании Shift (м/с)")]
        private float _sprintSpeed = 3.8f;
        
        [SerializeField, Tooltip("Время разгона до полной скорости (секунды)")]
        private float _accelerationTime = 0.35f;
        
        [SerializeField, Tooltip("Время остановки после отпускания клавиш (секунды)")]
        private float _decelerationTime = 0.25f;
        
        [SerializeField, Tooltip("Высота прыжка в метрах")]
        private float _jumpHeight = 0.9f;
        
        [SerializeField, Tooltip("Гравитация (отрицательное значение; чуть сильнее реальной)")]
        private float _gravity = -18.0f;
        
        [Header("Camera Look")]
        [SerializeField, Tooltip("Множитель чувствительности мыши")]
        private float _mouseSensitivity = 1.0f;
        
        [SerializeField, Tooltip("Минимальный угол взгляда вниз (градусы)")]
        private float _lookPitchMin = -85.0f;
        
        [SerializeField, Tooltip("Максимальный угол взгляда вверх (градусы)")]
        private float _lookPitchMax = 85.0f;
        
        [SerializeField, Tooltip("Инверсия мыши по вертикали")]
        private bool _invertMouseY = false;
        
        [Header("Head Bob")]
        [SerializeField, Tooltip("Включить покачивание камеры при ходьбе")]
        private bool _enableHeadBob = true;
        
        [SerializeField, Tooltip("Амплитуда покачивания при обычной ходьбе (метры)")]
        private float _walkBobAmplitude = 0.015f;
        
        [SerializeField, Tooltip("Частота покачивания при обычной ходьбе (Гц)")]
        private float _walkBobFrequency = 2.0f;
        
        [SerializeField, Tooltip("Амплитуда покачивания при беге (метры)")]
        private float _sprintBobAmplitude = 0.03f;
        
        [SerializeField, Tooltip("Частота покачивания при беге (Гц)")]
        private float _sprintBobFrequency = 2.8f;
        
        [SerializeField, Tooltip("Скорость плавного включения/затухания покачивания")]
        private float _bobLerpSpeed = 8.0f;
        
        public float WalkSpeed => _walkSpeed;
        public float SprintSpeed => _sprintSpeed;
        public float AccelerationTime => _accelerationTime;
        public float DecelerationTime => _decelerationTime;
        public float JumpHeight => _jumpHeight;
        public float Gravity => _gravity;
        public float MouseSensitivity => _mouseSensitivity;
        public float LookPitchMin => _lookPitchMin;
        public float LookPitchMax => _lookPitchMax;
        public bool InvertMouseY => _invertMouseY;
        
        public bool EnableHeadBob => _enableHeadBob;
        public float WalkBobAmplitude => _walkBobAmplitude;
        public float WalkBobFrequency => _walkBobFrequency;
        public float SprintBobAmplitude => _sprintBobAmplitude;
        public float SprintBobFrequency => _sprintBobFrequency;
        public float BobLerpSpeed => _bobLerpSpeed;
        
        private void OnValidate()
        {
            // Движение
            _walkSpeed = Mathf.Max(0.1f, _walkSpeed);
            _sprintSpeed = Mathf.Max(_walkSpeed, _sprintSpeed);
            _accelerationTime = Mathf.Max(0.01f, _accelerationTime);
            _decelerationTime = Mathf.Max(0.01f, _decelerationTime);
            _jumpHeight = Mathf.Max(0.0f, _jumpHeight);
            _gravity = Mathf.Min(-0.1f, _gravity); // отрицательное значение
            
            // Камера
            _mouseSensitivity = Mathf.Max(0.01f, _mouseSensitivity);
            _lookPitchMin = Mathf.Clamp(_lookPitchMin, -90f, 90f);
            _lookPitchMax = Mathf.Clamp(_lookPitchMax, -90f, 90f);
            if (_lookPitchMax < _lookPitchMin)
            {
                _lookPitchMax = _lookPitchMin;
            }
            
            // Head Bob
            _walkBobAmplitude = Mathf.Clamp(_walkBobAmplitude, 0f, 0.1f);
            _walkBobFrequency = Mathf.Clamp(_walkBobFrequency, 0.5f, 10f);
            _sprintBobAmplitude = Mathf.Clamp(_sprintBobAmplitude, 0f, 0.1f);
            _sprintBobFrequency = Mathf.Clamp(_sprintBobFrequency, 0.5f, 10f);
            _bobLerpSpeed = Mathf.Max(0.1f, _bobLerpSpeed);
        }
    }
}