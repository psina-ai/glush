using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Ink.Runtime;
using Glush.Personality;

namespace Glush.Dialogue
{
    public class InkDialogueRunner : MonoBehaviour
    {
        public static InkDialogueRunner Instance { get; private set; }

        private Story _currentStory;
        private readonly List<Ink.Runtime.Choice> _currentChoices = new();

        public bool IsDialogueActive => _currentStory != null;
        public string CurrentText { get; private set; } = string.Empty;
        public IReadOnlyList<Ink.Runtime.Choice> CurrentChoices => _currentChoices;

        public static event Action OnDialogueStarted;
        public static event Action OnDialogueContinued;
        public static event Action OnDialogueEnded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void StartDialogue(TextAsset inkJson)
        {
            if (inkJson == null)
            {
                Debug.LogError("InkDialogueRunner: inkJson равен null");
                return;
            }

            if (IsDialogueActive)
            {
                Debug.LogWarning("InkDialogueRunner: диалог уже активен");
                return;
            }

            _currentStory = new Story(inkJson.text);
            RegisterExternalFunctions();
            OnDialogueStarted?.Invoke();
            ContinueStory();
        }

        public void MakeChoice(int choiceIndex)
        {
            if (!IsDialogueActive)
            {
                Debug.LogWarning("InkDialogueRunner: попытка выбрать вариант при неактивном диалоге");
                return;
            }

            if (choiceIndex < 0 || choiceIndex >= _currentChoices.Count)
            {
                Debug.LogWarning($"InkDialogueRunner: индекс {choiceIndex} вне диапазона [0, {_currentChoices.Count - 1}]");
                return;
            }

            _currentStory.ChooseChoiceIndex(choiceIndex);
            ContinueStory();
        }

        public void EndDialogue()
        {
            _currentStory = null;
            CurrentText = string.Empty;
            _currentChoices.Clear();
            OnDialogueEnded?.Invoke();
        }

        private void ContinueStory()
        {
            var builder = new StringBuilder();

            while (_currentStory.canContinue)
            {
                builder.AppendLine(_currentStory.Continue());
            }

            CurrentText = builder.ToString().TrimEnd();
            _currentChoices.Clear();
            _currentChoices.AddRange(_currentStory.currentChoices);

            if (!_currentStory.canContinue && _currentChoices.Count == 0)
            {
                EndDialogue();
                return;
            }
            Debug.Log($"[Runner] ContinueStory: text='{CurrentText}', choices={_currentChoices.Count}");
            OnDialogueContinued?.Invoke();
        }

        /// <summary>
        /// Регистрация внешних функций Ink для чтения и изменения шкал личности.
        /// Вызывается при старте каждого нового диалога.
        /// </summary>
        private void RegisterExternalFunctions()
        {
            if (_currentStory == null) return;

            // Проверка доступности PersonalityManager
            if (PersonalityManager.Instance == null)
            {
                Debug.LogWarning("[Personality] PersonalityManager.Instance == null, внешние функции не зарегистрированы.");
                return;
            }

            var data = PersonalityManager.Instance.Data;

            // Функции чтения
            _currentStory.BindExternalFunction("get_zhit", () => data.Get(PersonalityAxis.Zhiteyskaya));
            _currentStory.BindExternalFunction("get_pov", () => data.Get(PersonalityAxis.Povedencheskaya));
            _currentStory.BindExternalFunction("get_poet", () => data.Get(PersonalityAxis.Poetichnaya));

            // Функции изменения
            _currentStory.BindExternalFunction("shift_zhit", (int delta) =>
            {
                int oldValue = data.Get(PersonalityAxis.Zhiteyskaya);
                data.Add(PersonalityAxis.Zhiteyskaya, delta);
                int newValue = data.Get(PersonalityAxis.Zhiteyskaya);
                Debug.Log($"[Personality] shift_zhit({delta}): Zhiteyskaya {oldValue} -> {newValue}");
            });

            _currentStory.BindExternalFunction("shift_pov", (int delta) =>
            {
                int oldValue = data.Get(PersonalityAxis.Povedencheskaya);
                data.Add(PersonalityAxis.Povedencheskaya, delta);
                int newValue = data.Get(PersonalityAxis.Povedencheskaya);
                Debug.Log($"[Personality] shift_pov({delta}): Povedencheskaya {oldValue} -> {newValue}");
            });

            _currentStory.BindExternalFunction("shift_poet", (int delta) =>
            {
                int oldValue = data.Get(PersonalityAxis.Poetichnaya);
                data.Add(PersonalityAxis.Poetichnaya, delta);
                int newValue = data.Get(PersonalityAxis.Poetichnaya);
                Debug.Log($"[Personality] shift_poet({delta}): Poetichnaya {oldValue} -> {newValue}");
            });

            Debug.Log("[Personality] Внешние функции зарегистрированы: get_zhit, get_pov, get_poet, shift_zhit, shift_pov, shift_poet");
        }

        // ═══════════════════════════════════════════════════════
        // Отладочный запуск диалога через ContextMenu инспектора
        // Правый клик на компоненте → TEST: Start Test Dialogue
        // ═══════════════════════════════════════════════════════

        [SerializeField] private TextAsset _testDialogueJson;

        [ContextMenu("TEST: Start Test Dialogue")]
        private void ContextTestStartDialogue()
        {
            if (_testDialogueJson == null)
            {
                Debug.LogError("[Test] Не назначен _testDialogueJson в инспекторе");
                return;
            }
            StartDialogue(_testDialogueJson);
        }

        [ContextMenu("TEST: Force End Dialogue")]
        private void ContextTestEndDialogue()
        {
            if (IsDialogueActive) EndDialogue();
            else Debug.Log("[Test] Диалог не активен");
        }
    }
}