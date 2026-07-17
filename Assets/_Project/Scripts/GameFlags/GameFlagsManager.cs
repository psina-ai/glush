using UnityEngine;
using System.IO;

namespace Glush.GameFlags
{
    /// <summary>
    /// Менеджер флагов состояния мира — синглтон, отвечающий за жизненный цикл и сохранение.
    /// Вешается на один GameObject в сцене.
    /// </summary>
    public class GameFlagsManager : MonoBehaviour
    {
        public static GameFlagsManager Instance { get; private set; }

        [SerializeField] private string _saveFileName = "game_flags.json";

        public GameFlags Flags { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // Создаём свежие данные (все пусто)
            Flags = new GameFlags();

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
            string json = JsonUtility.ToJson(Flags, prettyPrint: true);
            string path = Path.Combine(Application.persistentDataPath, _saveFileName);
            File.WriteAllText(path, json);
            Debug.Log($"[GameFlags] Saved to {path}");
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
                JsonUtility.FromJsonOverwrite(json, Flags);
                Debug.Log($"[GameFlags] Loaded from {path}");
            }
            else
            {
                Debug.Log($"[GameFlags] No save file at {path}, using fresh data");
            }
        }

        /// <summary>
        /// Сбросить все флаги и удалить файл сохранения.
        /// </summary>
        public void NewGame()
        {
            Flags.ResetAll();
            string path = Path.Combine(Application.persistentDataPath, _saveFileName);
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log($"[GameFlags] Deleted save file at {path}");
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
