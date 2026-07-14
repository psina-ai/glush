using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Glush.Dialogue
{
    public class DialogueUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject _dialogueRoot;
        [SerializeField] private RectTransform _historyContainer;
        [SerializeField] private RectTransform _choicesContainer;
        [SerializeField] private ScrollRect _historyScrollRect;
        [SerializeField] private CanvasGroup _blackOverlay;

        [Header("Prefabs")]
        [SerializeField] private TMPro.TMP_Text _historyLinePrefab;
        [SerializeField] private Button _choiceButtonPrefab;

        [Header("Fade Settings")]
        [SerializeField, Range(0.05f, 1f)] private float _fadeInDuration = 0.15f;
        [SerializeField, Range(0.05f, 1f)] private float _fadeOutDuration = 0.2f;
        [SerializeField, Range(0.3f, 1f)] private float _oldLineAlpha = 0.5f;

        [Header("Player References")]
        [SerializeField] private Behaviour _playerMovement;
        [SerializeField] private Behaviour _playerCameraLook;

        private readonly List<TMPro.TMP_Text> _spawnedHistoryLines = new();
        private readonly List<Button> _spawnedChoiceButtons = new();

        private void Awake()
        {
            _dialogueRoot.SetActive(false);
            _blackOverlay.alpha = 0f;
        }

        private void OnEnable()
        {
            InkDialogueRunner.OnDialogueStarted += HandleDialogueStarted;
            InkDialogueRunner.OnDialogueContinued += HandleDialogueContinued;
            InkDialogueRunner.OnDialogueEnded += HandleDialogueEnded;
        }

        private void OnDisable()
        {
            InkDialogueRunner.OnDialogueStarted -= HandleDialogueStarted;
            InkDialogueRunner.OnDialogueContinued -= HandleDialogueContinued;
            InkDialogueRunner.OnDialogueEnded -= HandleDialogueEnded;
        }

        private void HandleDialogueStarted()
        {
            StartCoroutine(OpenSequence());
        }

        private void HandleDialogueContinued()
        {
            UpdateHistory();
            UpdateChoices();
        }

        private void HandleDialogueEnded()
        {
            StartCoroutine(CloseSequence());
        }

        private IEnumerator OpenSequence()
        {
            LockPlayer(true);
            yield return StartCoroutine(FadeOverlay(0f, 1f, _fadeInDuration));
            
            _dialogueRoot.SetActive(true);
            ClearHistory();
            ClearChoices();
            
            yield return StartCoroutine(FadeOverlay(1f, 0f, _fadeOutDuration));
        }

        private IEnumerator CloseSequence()
        {
            yield return StartCoroutine(FadeOverlay(0f, 1f, _fadeInDuration));
            
            ClearHistory();
            ClearChoices();
            _dialogueRoot.SetActive(false);
            LockPlayer(false);
            
            yield return StartCoroutine(FadeOverlay(1f, 0f, _fadeOutDuration));
        }

        private IEnumerator FadeOverlay(float from, float to, float duration)
        {
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _blackOverlay.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            
            _blackOverlay.alpha = to;
        }

        private void LockPlayer(bool locked)
        {
            if (_playerMovement != null)
                _playerMovement.enabled = !locked;
            
            if (_playerCameraLook != null)
                _playerCameraLook.enabled = !locked;
            
            Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = locked;
        }

        private void UpdateHistory()
        {
            foreach (var line in _spawnedHistoryLines)
            {
                var color = line.color;
                color.a = _oldLineAlpha;
                line.color = color;
            }

            var newLine = Instantiate(_historyLinePrefab, _historyContainer);
            newLine.text = InkDialogueRunner.Instance.CurrentText;
            _spawnedHistoryLines.Add(newLine);

            LayoutRebuilder.ForceRebuildLayoutImmediate(_historyContainer);
            
            if (_historyScrollRect != null)
                _historyScrollRect.verticalNormalizedPosition = 0f;
        }

        private void UpdateChoices()
        {
            ClearChoices();

            var choices = InkDialogueRunner.Instance.CurrentChoices;
            
            for (int i = 0; i < choices.Count; i++)
            {
                var button = Instantiate(_choiceButtonPrefab, _choicesContainer);
                var text = button.GetComponentInChildren<TMPro.TMP_Text>();
                
                if (text != null)
                    text.text = choices[i].text;
                
                int index = i;
                button.onClick.AddListener(() => OnChoiceClicked(index));
                
                _spawnedChoiceButtons.Add(button);
            }
        }

        private void OnChoiceClicked(int choiceIndex)
        {
            ClearChoices();
            InkDialogueRunner.Instance.MakeChoice(choiceIndex);
        }

        private void ClearHistory()
        {
            foreach (var line in _spawnedHistoryLines)
                Destroy(line.gameObject);
            
            _spawnedHistoryLines.Clear();
        }

        private void ClearChoices()
        {
            foreach (var button in _spawnedChoiceButtons)
            {
                if (button != null)
                    Destroy(button.gameObject);
            }
            
            _spawnedChoiceButtons.Clear();
        }
    }
}