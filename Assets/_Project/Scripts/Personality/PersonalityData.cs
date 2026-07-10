using UnityEngine;

namespace Glush.Personality
{
    /// <summary>
    /// Данные личности — текущие значения трёх шкал.
    /// Сериализуется для сохранения.
    /// </summary>
    [System.Serializable]
    public class PersonalityData
    {
        [SerializeField] private int _zhiteyskaya = 0;
        [SerializeField] private int _povedencheskaya = 0;
        [SerializeField] private int _poetichnaya = 0;

        /// <summary>
        /// Получить текущее значение оси.
        /// </summary>
        public int Get(PersonalityAxis axis)
        {
            switch (axis)
            {
                case PersonalityAxis.Zhiteyskaya: return _zhiteyskaya;
                case PersonalityAxis.Povedencheskaya: return _povedencheskaya;
                case PersonalityAxis.Poetichnaya: return _poetichnaya;
                default: return 0;
            }
        }

        /// <summary>
        /// Установить значение оси с автоматическим ограничением в диапазон [-100, +100].
        /// </summary>
        public void Set(PersonalityAxis axis, int value)
        {
            value = Mathf.Clamp(value, -100, 100);
            switch (axis)
            {
                case PersonalityAxis.Zhiteyskaya:
                    _zhiteyskaya = value;
                    break;
                case PersonalityAxis.Povedencheskaya:
                    _povedencheskaya = value;
                    break;
                case PersonalityAxis.Poetichnaya:
                    _poetichnaya = value;
                    break;
            }
        }

        /// <summary>
        /// Прибавить дельту к значению оси с ограничением.
        /// </summary>
        public void Add(PersonalityAxis axis, int delta)
        {
            int current = Get(axis);
            Set(axis, current + delta);
        }

        /// <summary>
        /// Сбросить все шкалы к нулю.
        /// </summary>
        public void ResetAll()
        {
            _zhiteyskaya = 0;
            _povedencheskaya = 0;
            _poetichnaya = 0;
        }
    }
}