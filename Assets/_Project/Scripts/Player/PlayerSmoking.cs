using UnityEngine;
using Glush.GameFlags;
using Glush.Dialogue;

namespace Glush.Player
{
    /// <summary>
    /// Расход сигареты по клавише C (см. GameFlagKeys.CigarettesCount).
    /// Курить нельзя во время активного диалога и если сигарет нет —
    /// в этом случае ничего не расходуется, только лог в консоль.
    /// При успешном расходе запускает короткий Ink-монолог через InkDialogueRunner.
    /// </summary>
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerSmoking : MonoBehaviour
    {
        [SerializeField] private TextAsset _smokingDialogueJson;
        [SerializeField] private string _storyStateId = "smoking_default";
        [SerializeField] private string _entryKnot = "smoking_entry";

        private PlayerInput _playerInput;

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
        }

        private void OnEnable()
        {
            _playerInput.OnSmokePressed += HandleSmokePressed;
        }

        private void OnDisable()
        {
            _playerInput.OnSmokePressed -= HandleSmokePressed;
        }

        private void HandleSmokePressed()
        {
            if (InkDialogueRunner.Instance == null || GameFlagsManager.Instance == null)
            {
                Debug.LogWarning("[Smoking] InkDialogueRunner или GameFlagsManager не найдены в сцене");
                return;
            }

            // Пока идёт другой диалог — курить нельзя
            if (InkDialogueRunner.Instance.IsDialogueActive)
            {
                return;
            }

            var flags = GameFlagsManager.Instance.Flags;
            int cigarettesCount = flags.GetInt(GameFlagKeys.CigarettesCount);

            if (cigarettesCount <= 0)
            {
                Debug.Log("[Smoking] Сигарет нет. Нужно купить у кассирши в Пятёрочке.");
                return;
            }

            flags.AddInt(GameFlagKeys.CigarettesCount, -1);
            GameFlagsManager.Instance.Save();
            Debug.Log($"[Smoking] Выкурил одну. Осталось: {flags.GetInt(GameFlagKeys.CigarettesCount)}");

            if (_smokingDialogueJson == null)
            {
                Debug.LogError("[Smoking] Не назначен _smokingDialogueJson в инспекторе PlayerSmoking");
                return;
            }

            InkDialogueRunner.Instance.RememberStoryStateId(_storyStateId);
            InkDialogueRunner.Instance.StartDialogue(_smokingDialogueJson, _storyStateId, _entryKnot);
        }
    }
}
