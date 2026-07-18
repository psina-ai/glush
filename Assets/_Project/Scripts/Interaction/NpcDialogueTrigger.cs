using System.Collections;
using Glush.Dialogue;
using Unity.Cinemachine;
using UnityEngine;

namespace Glush.Interaction
{
    public class NpcDialogueTrigger : Interactable
    {
        [SerializeField] private TextAsset _dialogueJson;
        [SerializeField] private string _promptText = "Поговорить";
        [SerializeField] private CinemachineCamera _dialogueCamera;
        [SerializeField] private float _cameraSwitchDelay = 0.35f;
        [SerializeField] private int _dialoguePriorityBoost = 10;
        [SerializeField] private string _storyStateId;
        [SerializeField] private string _entryKnot;

        private Coroutine _cameraSwitchCoroutine;
        private int _defaultCameraPriority;
        private bool _ownsActiveDialogue;

        private void Awake()
        {
            if (_dialogueCamera != null)
            {
                _defaultCameraPriority = _dialogueCamera.Priority.Value;
            }
        }

        private void OnEnable()
        {
            InkDialogueRunner.OnDialogueEnded += HandleDialogueEnded;
        }

        private void OnDisable()
        {
            InkDialogueRunner.OnDialogueEnded -= HandleDialogueEnded;

            if (_cameraSwitchCoroutine != null)
            {
                StopCoroutine(_cameraSwitchCoroutine);
                _cameraSwitchCoroutine = null;
            }

            if (_ownsActiveDialogue && _dialogueCamera != null)
            {
                _dialogueCamera.Priority = _defaultCameraPriority;
            }

            _ownsActiveDialogue = false;
        }

        public override string GetPrompt()
        {
            return _promptText;
        }

        public override void Interact(GameObject interactor)
        {
            if (_dialogueJson == null)
            {
                Debug.LogError($"NpcDialogueTrigger ({name}): не назначен _dialogueJson", this);
                return;
            }

            if (InkDialogueRunner.Instance == null)
            {
                Debug.LogError($"NpcDialogueTrigger ({name}): InkDialogueRunner отсутствует в сцене", this);
                return;
            }

            if (InkDialogueRunner.Instance.IsDialogueActive)
            {
                Debug.LogWarning($"NpcDialogueTrigger ({name}): диалог уже активен", this);
                return;
            }

            // Триггер должен возвращать только ту камеру, которую активировал сам.
            _ownsActiveDialogue = true;

            InkDialogueRunner.Instance.RememberStoryStateId(_storyStateId);
            InkDialogueRunner.Instance.StartDialogue(_dialogueJson, _storyStateId, _entryKnot);

            ScheduleCameraPriority(_defaultCameraPriority + _dialoguePriorityBoost);
        }

        private void HandleDialogueEnded()
        {
            // Событие глобальное: монологи и другие NPC не должны менять эту камеру.
            if (!_ownsActiveDialogue)
            {
                return;
            }

            _ownsActiveDialogue = false;
            ScheduleCameraPriority(_defaultCameraPriority);
        }

        private void ScheduleCameraPriority(int targetPriority)
        {
            if (_dialogueCamera == null)
            {
                return;
            }

            if (_cameraSwitchCoroutine != null)
            {
                StopCoroutine(_cameraSwitchCoroutine);
            }

            _cameraSwitchCoroutine = StartCoroutine(
                SwitchCameraDelayed(targetPriority, _cameraSwitchDelay));
        }

        private IEnumerator SwitchCameraDelayed(int targetPriority, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);

            int previousPriority = _dialogueCamera.Priority.Value;
            _dialogueCamera.Priority = targetPriority;
            _cameraSwitchCoroutine = null;

            Debug.Log(
                $"[Camera] {_dialogueCamera.name}: приоритет {previousPriority} → {targetPriority}");
        }
    }
}

