using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Ink.Runtime;
using Glush.Personality;
using Glush.GameFlags;
using IOFile = System.IO.File;
using IOPath = System.IO.Path;

namespace Glush.Dialogue
{
    [System.Serializable]
    internal class InkStateEntry
    {
        public string id;
        public string stateJson;
    }

    [System.Serializable]
    internal class InkStateData
    {
        public List<InkStateEntry> entries = new();
    }

    /// <summary>
    /// Singleton-менеджер сохранения Ink-состояний между разговорами и перезапусками.
    /// Хранит сериализованные состояния Story.state по string ID в ink_states.json.
    /// </summary>
    public class InkStateManager : MonoBehaviour
    {
        public static InkStateManager Instance { get; private set; }

        private const string _saveFileName = "ink_states.json";
        private readonly Dictionary<string, string> _states = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
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
        /// Получить сохранённый JSON состояния Ink-истории по ID.
        /// </summary>
        public bool TryGetState(string storyStateId, out string stateJson)
        {
            if (_states.TryGetValue(storyStateId, out stateJson))
            {
                return true;
            }

            stateJson = null;
            return false;
        }

        /// <summary>
        /// Сохранить JSON состояния Ink-истории по ID.
        /// </summary>
        public void SetState(string storyStateId, string stateJson)
        {
            if (string.IsNullOrEmpty(storyStateId))
            {
                Debug.LogWarning("InkStateManager: пустой storyStateId, состояние не сохранено");
                return;
            }

            _states[storyStateId] = stateJson;
        }

        /// <summary>
        /// Сохранить все состояния в ink_states.json.
        /// </summary>
        public void Save()
        {
            var data = new InkStateData();
            foreach (var kvp in _states)
            {
                data.entries.Add(new InkStateEntry { id = kvp.Key, stateJson = kvp.Value });
            }

            string path = IOPath.Combine(Application.persistentDataPath, _saveFileName);
            IOFile.WriteAllText(path, JsonUtility.ToJson(data, prettyPrint: true));
            Debug.Log($"[InkState] Saved to {path}, entries={_states.Count}");
        }

        /// <summary>
        /// Загрузить все состояния из ink_states.json.
        /// </summary>
        public void Load()
        {
            _states.Clear();

            string path = IOPath.Combine(Application.persistentDataPath, _saveFileName);
            if (!IOFile.Exists(path))
            {
                Debug.Log("[InkState] No save file at " + path + ", using fresh data");
                return;
            }

            try
            {
                string json = IOFile.ReadAllText(path);
                var data = JsonUtility.FromJson<InkStateData>(json);

                if (data != null && data.entries != null)
                {
                    foreach (var entry in data.entries)
                    {
                        if (!string.IsNullOrEmpty(entry.id))
                        {
                            _states[entry.id] = entry.stateJson;
                        }
                    }
                }

                Debug.Log($"[InkState] Loaded from {path}, entries={_states.Count}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[InkState] Failed to load {path}: {e.Message}");
            }
        }

        /// <summary>
        /// Сбросить все состояния в памяти и удалить файл сохранения.
        /// </summary>
        public void ResetAll()
        {
            _states.Clear();

            string path = IOPath.Combine(Application.persistentDataPath, _saveFileName);
            if (IOFile.Exists(path))
            {
                IOFile.Delete(path);
                Debug.Log($"[InkState] Deleted save file at {path}");
            }
            else
            {
                Debug.Log("[InkState] No save file to delete");
            }
        }

        [ContextMenu("Reset All Ink States")]
        private void ResetAllMenu() => ResetAll();
    }
}
