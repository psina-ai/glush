using UnityEngine;
using Glush.Personality;

namespace Glush.Interaction
{
    /// <summary>
    /// Тестовый интерактивный объект для проверки системы взаимодействия
    /// Меняет цвет при взаимодействии и подсвечивается при фокусе
    /// </summary>
    public class TestInteractable : Interactable
    {
        [Header("Test Settings")]
        [SerializeField, Tooltip("Текст промпта для UI")]
        private string _prompt = "Нажать кнопку";
        
        [SerializeField, Tooltip("Сообщение в консоль при взаимодействии")]
        private string _actionMessage = "Вы взаимодействовали с объектом.";
        
        [Header("Visual Feedback")]
        [SerializeField, Tooltip("Цвет highlight при фокусе")]
        private Color _focusColor = Color.yellow;
        
        [SerializeField, Tooltip("Интенсивность highlight при фокусе")]
        private float _focusIntensity = 1.5f;

        [Header("Personality Test")]
        [SerializeField, Tooltip("Применять сдвиг к шкалам личности при взаимодействии")]
        private bool _applyPersonalityShift = false;

        [SerializeField, Tooltip("Величина сдвига для каждой шкалы")]
        private int _shiftAmount = 1;
        
        private Renderer _renderer;
        private Color _originalColor;
        private MaterialPropertyBlock _propertyBlock;
        
        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            if (_renderer != null)
            {
                _originalColor = _renderer.material.color;
                _propertyBlock = new MaterialPropertyBlock();
                _renderer.GetPropertyBlock(_propertyBlock);
            }
            
            // Подписка на события фокуса для визуальной обратной связи
            OnFocused += HandleFocusGained;
            OnUnfocused += HandleFocusLost;
        }
        
        public override string GetPrompt() => _prompt;
        
        public override void Interact(GameObject interactor)
        {
            Debug.Log(_actionMessage);
            
            // Визуальная обратная связь: случайный цвет
            if (_renderer != null)
            {
                _renderer.material.color = new Color(
                    Random.Range(0.2f, 1f),
                    Random.Range(0.2f, 1f),
                    Random.Range(0.2f, 1f)
                );
            }

            // Тестирование системы личности
            if (_applyPersonalityShift && PersonalityManager.Instance != null)
            {
                var data = PersonalityManager.Instance.Data;
                data.Add(PersonalityAxis.Zhiteyskaya, _shiftAmount);
                data.Add(PersonalityAxis.Povedencheskaya, _shiftAmount);
                data.Add(PersonalityAxis.Poetichnaya, _shiftAmount);
                Debug.Log($"[Personality] Zh:{data.Get(PersonalityAxis.Zhiteyskaya)} " +
                          $"Po:{data.Get(PersonalityAxis.Povedencheskaya)} " +
                          $"Pt:{data.Get(PersonalityAxis.Poetichnaya)}");
            }
        }
        
        private void HandleFocusGained()
        {
            if (_renderer == null) return;
            
            // Подсветка объекта при фокусе
            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor("_EmissionColor", _focusColor * _focusIntensity);
            _renderer.SetPropertyBlock(_propertyBlock);
        }
        
        private void HandleFocusLost()
        {
            if (_renderer == null) return;
            
            // Возвращаем обычный вид
            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor("_EmissionColor", Color.black);
            _renderer.SetPropertyBlock(_propertyBlock);
        }
        
        private void OnDestroy()
        {
            // Отписка от событий при уничтожении
            OnFocused -= HandleFocusGained;
            OnUnfocused -= HandleFocusLost;
            
            // Возвращаем оригинальный цвет
            if (_renderer != null)
            {
                _renderer.material.color = _originalColor;
            }
        }
    }
}