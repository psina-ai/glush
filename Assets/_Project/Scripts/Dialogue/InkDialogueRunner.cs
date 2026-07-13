using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using Ink.Runtime;

namespace Glush.Dialogue
{
    public class InkDialogueRunner : MonoBehaviour
    {
        public static InkDialogueRunner Instance { get; private set; }

        [SerializeField] private TextAsset _testInkJson;

        private Story _currentStory;
        private readonly List<Choice> _currentChoices = new List<Choice>();

        public bool IsDialogueActive => _currentStory != null;
        public string CurrentText { get; private set; } = string.Empty;
        public IReadOnlyList<Choice> CurrentChoices => _currentChoices;

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
                Instance = null;
        }

        public void StartDialogue(TextAsset inkJson)
        {
            if (inkJson == null)
            {
                Debug.LogError("[Dialogue] StartDialogue: inkJson is null");
                return;
            }
            if (IsDialogueActive)
            {
                Debug.LogWarning("[Dialogue] Диалог уже активен, StartDialogue проигнорирован");
                return;
            }

            _currentStory = new Story(inkJson.text);
            ContinueStory();
        }

        public void MakeChoice(int choiceIndex)
        {
            if (!IsDialogueActive)
            {
                Debug.LogWarning("[Dialogue] MakeChoice: диалог не активен");
                return;
            }
            if (choiceIndex < 0 || choiceIndex >= _currentChoices.Count)
            {
                Debug.LogWarning($"[Dialogue] MakeChoice: недопустимый индекс {choiceIndex} (доступно: 0..{_currentChoices.Count - 1})");
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
            Debug.Log("[Dialogue] End");
        }

private void ContinueStory()
{
    var textBuilder = new StringBuilder();

    while (_currentStory.canContinue)
    {
        textBuilder.AppendLine(_currentStory.Continue());
    }

    CurrentText = textBuilder.ToString().Trim();
    _currentChoices.Clear();
    _currentChoices.AddRange(_currentStory.currentChoices);

    // ДИАГНОСТИКА — временная, потом удалим
    Debug.Log($"[Dialogue DEBUG] canContinue={_currentStory.canContinue}, choices={_currentChoices.Count}, textLen={CurrentText.Length}");

    if (!_currentStory.canContinue && _currentChoices.Count == 0)
    {
        EndDialogue();
    }
    else
    {
        LogCurrentState();
    }
}

        private void LogCurrentState()
        {
            Debug.Log("[Dialogue] ─────────────");
            Debug.Log($"[Dialogue] {CurrentText}");
            if (_currentChoices.Count > 0)
            {
                Debug.Log("[Dialogue] Варианты:");
                for (int i = 0; i < _currentChoices.Count; i++)
                    Debug.Log($"[Dialogue]   {i + 1}. {_currentChoices[i].text}");
            }
            else
            {
                Debug.Log("[Dialogue] (нет вариантов — конец фрагмента)");
            }
            Debug.Log("[Dialogue] ─────────────");
        }

        private void Update()
        {
            if (Keyboard.current == null) return;

            if (Keyboard.current.fKey.wasPressedThisFrame)
            {
                if (!IsDialogueActive)
                    StartDialogue(_testInkJson);
            }

            if (Keyboard.current.digit1Key.wasPressedThisFrame) MakeChoice(0);
            if (Keyboard.current.digit2Key.wasPressedThisFrame) MakeChoice(1);
            if (Keyboard.current.digit3Key.wasPressedThisFrame) MakeChoice(2);
            if (Keyboard.current.digit4Key.wasPressedThisFrame) MakeChoice(3);
        }
    }
}