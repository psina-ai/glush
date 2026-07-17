namespace Glush.GameFlags
{
    /// <summary>
    /// Константы флагов состояния мира. Используются вместо строк для избежания опечаток.
    /// Преобразование в snake_case происходит автоматически при передаче в методы GameFlags.
    /// </summary>
    public static class GameFlagKeys
    {
        // ═══════════════════════════════════════════════════════
        // Встречи с NPC (булевы флаги)
        // ═══════════════════════════════════════════════════════

        /// <summary>Встречал ли кассиршу из Пятёрочки</summary>
        public const string MetCashier = "met_cashier";

        /// <summary>Встречал ли рыбака</summary>
        public const string MetFisherman = "met_fisherman";

        /// <summary>Встречал ли старика из сторожки</summary>
        public const string MetGuardian = "met_guardian";

        // ═══════════════════════════════════════════════════════
        // Скрытые знания (булевы флаги)
        // ═══════════════════════════════════════════════════════

        /// <summary>Узнал, что кассирша — нечто большее</summary>
        public const string KnowsCashierIsMonster = "knows_cashier_is_monster";

        /// <summary>Узнал о маяке и его аномалии</summary>
        public const string KnowsAboutAnomalyLighthouse = "knows_about_anomaly_lighthouse";

        /// <summary>Узнал о войне до Морской Глуши</summary>
        public const string KnowsAboutPastWar = "knows_about_past_war";

        // ═══════════════════════════════════════════════════════
        // Действия впервые (булевы флаги)
        // ═══════════════════════════════════════════════════════

        /// <summary>Ел сырую рыбу впервые</summary>
        public const string AteRawFishFirstTime = "ate_raw_fish_first_time";

        /// <summary>Выстрелил из пистолета впервые</summary>
        public const string ShotPistolFirstTime = "shot_pistol_first_time";

        /// <summary>Спал в доме впервые</summary>
        public const string SleptInHouseFirstTime = "slept_in_house_first_time";

        // ═══════════════════════════════════════════════════════
        // Ключевые сцены (булевы флаги)
        // ═══════════════════════════════════════════════════════

        /// <summary>Увидел флешбэк о войне</summary>
        public const string SawFlashbackWar = "saw_flashback_war";

        /// <summary>Увидел зловещее предзнаменование концовки</summary>
        public const string SawEndingOmen = "saw_ending_omen";

        // ═══════════════════════════════════════════════════════
        // Счётчики действий (целые числа)
        // ═══════════════════════════════════════════════════════

        /// <summary>Сколько раз посетил Пятёрочку</summary>
        public const string TimesVisitedPyaterochka = "times_visited_pyaterochka";

        /// <summary>Сколько раз поговорил с кассиршей</summary>
        public const string TimesTalkedToCashier = "times_talked_to_cashier";

        /// <summary>Сколько всего выстрелов сделал</summary>
        public const string TotalShotsFired = "total_shots_fired";

        /// <summary>Сколько дней пережил</summary>
        public const string DaysSurvived = "days_survived";

        // ═══════════════════════════════════════════════════════
        // Глобальные статусы (строки)
        // ═══════════════════════════════════════════════════════

        /// <summary>Текущее время суток: "dawn" | "day" | "dusk" | "night"</summary>
        public const string TimeOfDay = "time_of_day";

        /// <summary>Текущая погода: "clear" | "fog" | "rain"</summary>
        public const string Weather = "weather";

        /// <summary>Что ел Алексей в последний раз: "none" | "sandwich" | "vodka" | ...</summary>
        public const string AlexeiLastMeal = "alexei_last_meal";
    }
}
