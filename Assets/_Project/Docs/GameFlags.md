# Справочник GameFlags

> См. GDD §19.5. Таблица всех флагов состояния мира. Обновляется одновременно с добавлением нового флага в `GameFlagKeys.cs`.
> Перед созданием нового флага — искать здесь похожий.

## Булевы флаги

| Ключ | Константа в `GameFlagKeys` | Описание |
|---|---|---|
| `met_cashier` | `MetCashier` | Встречал ли кассиршу из Пятёрочки |
| `met_fisherman` | `MetFisherman` | Встречал ли рыбака |
| `met_guardian` | `MetGuardian` | Встречал ли старика из сторожки |
| `knows_cashier_is_monster` | `KnowsCashierIsMonster` | Узнал, что кассирша — нечто большее |
| `knows_about_anomaly_lighthouse` | `KnowsAboutAnomalyLighthouse` | Узнал о маяке и его аномалии |
| `knows_about_past_war` | `KnowsAboutPastWar` | Узнал о войне до Морской Глуши |
| `ate_raw_fish_first_time` | `AteRawFishFirstTime` | Ел сырую рыбу впервые |
| `shot_pistol_first_time` | `ShotPistolFirstTime` | Выстрелил из пистолета впервые |
| `slept_in_house_first_time` | `SleptInHouseFirstTime` | Спал в доме впервые |
| `saw_flashback_war` | `SawFlashbackWar` | Увидел флешбэк о войне |
| `saw_ending_omen` | `SawEndingOmen` | Увидел зловещее предзнаменование концовки |

## Счётчики (целые числа)

| Ключ | Константа в `GameFlagKeys` | Описание |
|---|---|---|
| `times_visited_pyaterochka` | `TimesVisitedPyaterochka` | Сколько раз посетил Пятёрочку |
| `times_talked_to_cashier` | `TimesTalkedToCashier` | Сколько раз поговорил с кассиршей |
| `total_shots_fired` | `TotalShotsFired` | Сколько всего выстрелов сделал |
| `days_survived` | `DaysSurvived` | Сколько дней пережил |

## Ресурсы (целые числа)

| Ключ | Константа в `GameFlagKeys` | Описание | Кто пишет | Кто читает |
|---|---|---|---|---|
| `cigarettes_count` | `CigarettesCount` | Сколько сигарет у Алексея сейчас | `New Ink.ink` (`int_add`, +5 при покупке у кассирши, только если было ≤0) | Ink-диалог кассирши (гейт повторной покупки), `PlayerSmoking.cs` (расход -1 по клавише C, гейт запуска монолога) |

## Строковые статусы

| Ключ | Константа в `GameFlagKeys` | Описание |
|---|---|---|
| `time_of_day` | `TimeOfDay` | Текущее время суток: `dawn` \| `day` \| `dusk` \| `night` |
| `weather` | `Weather` | Текущая погода: `clear` \| `fog` \| `rain` |
| `alexei_last_meal` | `AlexeiLastMeal` | Что ел Алексей в последний раз: `none` \| `sandwich` \| `vodka` \| ... |
