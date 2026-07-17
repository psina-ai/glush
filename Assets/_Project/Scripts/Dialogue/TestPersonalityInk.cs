using UnityEngine;
using Glush.Dialogue;
using Glush.Personality;

namespace Glush.Dialogue
{
    /// <summary>
    /// Тестовый скрипт для проверки интеграции Ink и Personality.
    /// При запуске игры автоматически запускает тестовый диалог.
    /// </summary>
    public class TestPersonalityInk : MonoBehaviour
    {
        [SerializeField] private TextAsset _testDialogueJson;

        private void Start()
        {
            if (_testDialogueJson == null)
            {
                Debug.LogError("TestPersonalityInk: _testDialogueJson не назначен в инспекторе.");
                return;
            }

            // Даем время для инициализации систем
            Invoke(nameof(StartTestDialogue), 1f);
        }

        private void StartTestDialogue()
        {
            Debug.Log("[TestPersonalityInk] Запуск тестового диалога...");
            Debug.Log($"[TestPersonalityInk] Начальное состояние: " +
                      $"Zh: {PersonalityManager.Instance.Data.Get(PersonalityAxis.Zhiteyskaya)}, " +
                      $"Po: {PersonalityManager.Instance.Data.Get(PersonalityAxis.Povedencheskaya)}, " +
                      $"Pt: {PersonalityManager.Instance.Data.Get(PersonalityAxis.Poetichnaya)}");

            if (InkDialogueRunner.Instance != null)
            {
                InkDialogueRunner.Instance.StartDialogue(_testDialogueJson, "test", "start");
            }
            else
            {
                Debug.LogError("[TestPersonalityInk] InkDialogueRunner.Instance == null");
            }
        }
    }
}