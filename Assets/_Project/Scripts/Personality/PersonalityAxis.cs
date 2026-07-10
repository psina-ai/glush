/// <summary>
/// Ось личности персонажа.
/// Все значения в диапазоне [-100, +100], где -100 — крайнее значение "влево", +100 — крайнее "вправо".
/// </summary>
namespace Glush.Personality
{
    /// <summary>
    /// Житейская ось: Эхо войны ↔ Фатализм.
    /// Магистральная — путь, финал, сложность механических проверок.
    /// </summary>
    public enum PersonalityAxis
    {
        /// <summary>Эхо войны ↔ Фатализм</summary>
        Zhiteyskaya,
        /// <summary>Приказ ↔ Своеволие</summary>
        Povedencheskaya,
        /// <summary>Трезвость ↔ Иллюзия</summary>
        Poetichnaya
    }
}