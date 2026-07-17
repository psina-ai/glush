using UnityEngine;
using System.Collections.Generic;

namespace Glush.GameFlags
{
    /// <summary>
    /// Хранилище флагов состояния мира — булевы флаги, целые счётчики и строковые статусы.
    /// Сериализуется для сохранения в JSON.
    /// </summary>
    [System.Serializable]
    public class GameFlags
    {
        [SerializeField] private SerializableBoolDict _boolFlags = new();
        [SerializeField] private SerializableIntDict _intFlags = new();
        [SerializeField] private SerializableStringDict _stringFlags = new();

        /// <summary>
        /// Получить булев флаг. Если ключа нет — вернёт false.
        /// </summary>
        public bool GetBool(string key)
        {
            if (_boolFlags.TryGetValue(key, out var value))
                return value;
            return false;
        }

        /// <summary>
        /// Установить булев флаг.
        /// </summary>
        public void SetBool(string key, bool value)
        {
            _boolFlags[key] = value;
        }

        /// <summary>
        /// Получить целочисленный флаг. Если ключа нет — вернёт 0.
        /// </summary>
        public int GetInt(string key)
        {
            if (_intFlags.TryGetValue(key, out var value))
                return value;
            return 0;
        }

        /// <summary>
        /// Установить целочисленный флаг.
        /// </summary>
        public void SetInt(string key, int value)
        {
            _intFlags[key] = value;
        }

        /// <summary>
        /// Прибавить дельту к целочисленному флагу.
        /// </summary>
        public void AddInt(string key, int delta)
        {
            int current = GetInt(key);
            SetInt(key, current + delta);
        }

        /// <summary>
        /// Получить строковый флаг. Если ключа нет — вернёт пустую строку.
        /// </summary>
        public string GetString(string key)
        {
            if (_stringFlags.TryGetValue(key, out var value))
                return value;
            return string.Empty;
        }

        /// <summary>
        /// Установить строковый флаг.
        /// </summary>
        public void SetString(string key, string value)
        {
            _stringFlags[key] = value ?? string.Empty;
        }

        /// <summary>
        /// Очистить все флаги.
        /// </summary>
        public void ResetAll()
        {
            _boolFlags.Clear();
            _intFlags.Clear();
            _stringFlags.Clear();
        }

        // Обёртки для сериализации Dictionary в JSON через JsonUtility
        [System.Serializable]
        private class SerializableBoolDict : SerializableDictionary<bool> { }

        [System.Serializable]
        private class SerializableIntDict : SerializableDictionary<int> { }

        [System.Serializable]
        private class SerializableStringDict : SerializableDictionary<string> { }

        [System.Serializable]
        private abstract class SerializableDictionary<T> : Dictionary<string, T>, ISerializationCallbackReceiver
        {
            [SerializeField] private List<string> _keys = new();
            [SerializeField] private List<T> _values = new();

            public void OnBeforeSerialize()
            {
                _keys.Clear();
                _values.Clear();
                foreach (var kvp in this)
                {
                    _keys.Add(kvp.Key);
                    _values.Add(kvp.Value);
                }
            }

            public void OnAfterDeserialize()
            {
                Clear();
                for (int i = 0; i < _keys.Count; i++)
                {
                    this[_keys[i]] = _values[i];
                }
            }
        }
    }
}
