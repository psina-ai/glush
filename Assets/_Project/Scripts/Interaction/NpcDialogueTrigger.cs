using UnityEngine;
using System.Collections;
using Unity.Cinemachine;
using Glush.Dialogue;

namespace Glush.Interaction
{
    public class NpcDialogueTrigger : Interactable
    {
        [SerializeField] private TextAsset _dialogueJson;
        [SerializeField] private string _promptText = "Поговорить";
        [SerializeField] private CinemachineCamera _dialogueCamera;
        [SerializeField] private float _cameraSwitchDelay = 0.35f;

        private void OnEnable()
        {
            InkDialogueRunner.OnDialogueEnded += HandleDialogueEnded;
        }

        private void OnDisable()
        {
            InkDialogueRunner.OnDialogueEnded -= HandleDialogueEnded;
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

            if (InkDialogueRunner.Instance.IsDialogueActive)
            {
                Debug.LogWarning($"NpcDialogueTrigger ({name}): диалог уже активен", this);
                return;
            }

            InkDialogueRunner.Instance.StartDialogue(_dialogueJson);
            StartCoroutine(SwitchCameraDelayed(+10, _cameraSwitchDelay));
        }

        private void HandleDialogueEnded()
        {
            StartCoroutine(SwitchCameraDelayed(-10, _cameraSwitchDelay));
        }

        private IEnumerator SwitchCameraDelayed(int delta, float delay)
        {
            yield return new WaitForSeconds(delay);
            BoostCameraPriority(delta);
        }

        private void BoostCameraPriority(int delta)
        {
            if (_dialogueCamera == null) return;

            int current = _dialogueCamera.Priority.Value;
            _dialogueCamera.Priority = current + delta;
            Debug.Log($"[Camera] {_dialogueCamera.name}: приоритет {current} → {current + delta}");
        }
    }
}