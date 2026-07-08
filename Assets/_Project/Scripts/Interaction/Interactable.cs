using UnityEngine;
using System;

namespace Glush.Interaction
{
    /// <summary>
    /// Абстрактный базовый класс для всех интерактивных объектов в игре.
    /// Наследуй от этого класса для создания интерактивных объектов: NPC, предметы, двери, триггеры.
    /// Пример использования:
    /// <code>
    /// public class DoorInteractable : Interactable
    /// {
    ///     [SerializeField] private string _prompt = "Открыть дверь";
    ///     public override string GetPrompt() => _prompt;
    ///     
    ///     public override void Interact(GameObject interactor)
    ///     {
    ///         // Открыть дверь, начать диалог, поднять предмет
    ///         Debug.Log("Дверь открыта");
    ///     }
    /// }
    /// </code>
    /// </summary>
    public abstract class Interactable : MonoBehaviour
    {
        private bool _isFocused = false;
        
        /// <summary>
        /// Событие вызывается, когда игрок смотрит на этот объект
        /// </summary>
        public event Action OnFocused;
        
        /// <summary>
        /// Событие вызывается, когда игрок перестал смотреть на этот объект
        /// </summary>
        public event Action OnUnfocused;
        
        /// <summary>
        /// Возвращает текст для UI-промпта (без префикса "[E]")
        /// </summary>
        public abstract string GetPrompt();
        
        /// <summary>
        /// Выполняет взаимодействие с объектом
        /// </summary>
        /// <param name="interactor">Объект, который взаимодействует (обычно игрок)</param>
        public abstract void Interact(GameObject interactor);
        
        /// <summary>
        /// Можно ли взаимодействовать с объектом сейчас?
        /// Переопредели этот метод для временной блокировки взаимодействия
        /// </summary>
        public virtual bool CanInteract(GameObject interactor) => true;
        
        /// <summary>
        /// Вызывается PlayerInteractor, когда игрок начинает смотреть на этот объект
        /// </summary>
        public void NotifyFocused()
        {
            if (_isFocused) return;
            
            _isFocused = true;
            OnFocused?.Invoke();
        }
        
        /// <summary>
        /// Вызывается PlayerInteractor, когда игрок перестаёт смотреть на этот объект
        /// </summary>
        public void NotifyUnfocused()
        {
            if (!_isFocused) return;
            
            _isFocused = false;
            OnUnfocused?.Invoke();
        }
        
        /// <summary>
        /// Текущее состояние фокуса (читай только)
        /// </summary>
        public bool IsFocused => _isFocused;
    }
}