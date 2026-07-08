using UnityEngine;
using System;
using TMPro;
using Glush.Interaction;

namespace Glush.UI
{
    public class InteractionPromptUI : MonoBehaviour
    {
        [SerializeField] private PlayerInteractor _interactor;
        [SerializeField] private GameObject _rootPanel;
        [SerializeField] private TMP_Text _promptText;
        
        private void OnEnable()
        {
            if (_interactor != null)
            {
                _interactor.OnFocusChanged += HandleFocusChanged;
            }
            else
            {
                Debug.LogWarning($"[InteractionPromptUI] PlayerInteractor не назначен на {name}");
            }
            
            // На старте скрываем панель
            if (_rootPanel != null)
            {
                _rootPanel.SetActive(false);
            }
        }
        
        private void OnDisable()
        {
            if (_interactor != null)
            {
                _interactor.OnFocusChanged -= HandleFocusChanged;
            }
        }
        
        private void HandleFocusChanged(Interactable interactable)
        {
            if (_rootPanel == null || _promptText == null)
            {
                Debug.LogWarning($"[InteractionPromptUI] RootPanel или PromptText не назначены на {name}");
                return;
            }
            
            if (interactable != null)
            {
                // Показываем панель и обновляем текст
                _rootPanel.SetActive(true);
                _promptText.text = $"[E] {interactable.GetPrompt()}";
            }
            else
            {
                // Скрываем панель
                _rootPanel.SetActive(false);
            }
        }
        
        private void Reset()
        {
            // Автоматическое заполнение ссылок при добавлении компонента
            if (_rootPanel == null && transform.childCount > 0)
            {
                _rootPanel = transform.GetChild(0).gameObject;
            }
            
            if (_promptText == null && _rootPanel != null)
            {
                _promptText = _rootPanel.GetComponentInChildren<TMP_Text>();
            }
        }
    }
}