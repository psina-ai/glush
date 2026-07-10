using UnityEngine;
using System.IO;

namespace Glush.Personality
{
    /// <summary>
    /// Менеджер личности — синглтон, отвечающий за жизненный цикл и сохранение шкал.
    /// Вешается на один GameObject в сцене.
    /// </summary>
    public class PersonalityManager : MonoBehaviour
    {
        public static PersonalityManager Instance { get; private set; }

        [SerializeField] private string _saveFileName = "personality.json";

        public PersonalityData Data { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // Создаём свежие данные (все нули)
            Data = new PersonalityData();

            // Пытаемся загрузить сохранение поверх
            Load();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Сохранить текущие данные в JSON-файл.
        /// </summary>
        public void Save()
        {
            string json = JsonUtility.ToJson(Data, prettyPrint: true);
            string path = Path.Combine(Application.persistentDataPath, _saveFileName);
            File.WriteAllText(path, json);
            Debug.Log($"[Personality] Saved to {path}");
        }

        /// <summary>
        /// Загрузить данные из JSON-файла, если он существует.
        /// Если файла нет — оставляет текущие данные неизменными.
        /// </summary>
        public void Load()
        {
            string path = Path.Combine(Application.persistentDataPath, _saveFileName);
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                JsonUtility.FromJsonOverwrite(json, Data);
                Debug.Log($"[Personality] Loaded from {path}");
            }
            else
            {
                Debug.Log($"[Personality] No save file at {path}, using fresh data");
            }
        }

        /// <summary>
        /// Сбросить все шкалы к нулю и удалить файл сохранения.
        /// </summary>
        public void NewGame()
        {
            Data.ResetAll();
            string path = Path.Combine(Application.persistentDataPath, _saveFileName);
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log($"[Personality] Deleted save file at {path}");
            }
        }

        [ContextMenu("Save")]
        private void SaveMenu() => Save();

        [ContextMenu("Load")]
        private void LoadMenu() => Load();

        [ContextMenu("New Game")]
        private void NewGameMenu() => NewGame();
    }
}